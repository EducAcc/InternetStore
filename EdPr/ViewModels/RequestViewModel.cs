using EdPr.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects.DataClasses;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Net;
using System.Net.Mail;

using Word = Microsoft.Office.Interop.Word;

namespace EdPr.ViewModels
{
    public class RequestViewModel : ViewModelBase
    {
        private EdPrEntities _db;
        private User _user;
        private UserService _userService;
        private ViewModelBase _profileControl;
        private decimal _discount;
        private decimal _cartTotal;
        private decimal _payment;

        private const int DISCOUNT_PERS = 10;

        public ViewModelBase ProfileControl
        {
            get => _profileControl;
            set
            {
                _profileControl = value;
                OnPropertyChanged();
            }
        }
        public decimal Discount
        {
            get => _discount;
            set
            {
                _discount = value;
                OnPropertyChanged();

                Payment = CartTotal - Discount;
            }
        }
        public decimal CartTotal
        {
            get => _cartTotal;
            set
            {
                _cartTotal = value;
                OnPropertyChanged();

                Discount = GetDiscount(DISCOUNT_PERS);
            }
        }
        public decimal Payment
        {
            get => _payment;
            set
            {
                _payment = value;
                OnPropertyChanged();
            }
        }

        public List<GoodsCart> Cart { get; set; }

        public ICommand ConfrimRequestCommand { get; }
        public ICommand GoBackCommand { get; }

        public event Action BackRequested;

        public RequestViewModel(EdPrEntities db, User user)
        {
            _db = db;
            _user = user;

            _userService = new UserService(_db);
            ProfileControl = new UserProfileViewModel(_userService, _user) { IsRequestMode = true};

            Cart = GetCartGoods();
            CartTotal = GetTotal();

            ConfrimRequestCommand = new RelayCommand(ExecuteConfirmRequest);
            GoBackCommand = new RelayCommand(_ => BackRequested?.Invoke());
        }

        private List<GoodsCart> GetCartGoods()
        {
            return _db.GoodsCarts.Where(x => x.CartId == _user.Id)
                .ToList();
        }

        private decimal GetDiscount(int disPersantage)
        {
            return CartTotal * disPersantage / 100; 
        }

        private decimal GetTotal()
        {
            return (decimal)_db.GoodsCarts.Where(x => x.CartId == _user.Id)?
                .Sum(x => x.Quantitty * x.Good.Price);
        }

        private void ExecuteConfirmRequest(object parameter)
        {
            if (_userService.ValidatePersonalDataFields(_user, out string error, true))
            {
                string number = DateTime.Now.ToString("yyyyMMddHHmmss");
                var now = DateTime.Now;

                try
                {
                    var newRequest = new Request
                    {
                        ClientId = _user.Id,
                        Date = now.Date,
                        Total = Payment
                    };

                    RequestGood newRequestGood = null;

                    foreach (var cartItem in Cart)
                    {
                        newRequestGood = new RequestGood
                        {
                            Request = newRequest,
                            GoodId = cartItem.GoodId,
                            Quantity = cartItem.Quantitty
                        };

                        _db.RequestGoods.Add(newRequestGood);
                    }

                    _db.Requests.Add(newRequest);

                    _db.SaveChanges();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при добавлении заказа: {ex.Message}");
                    return;
                }

                string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Чеки");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string templatePath = Path.GetFullPath("Шаблон чека.docx");
                string outputPath = Path.Combine(folderPath, $"Чек №{number}.docx");

                var wordApp = new Word.Application();
                wordApp.Visible = false;

                Word.Document doc = null;

                bool isSuccessful = false;

                try
                {
                    doc = wordApp.Documents.Open(templatePath);

                    Repwo("{Номер}", number.ToString(), doc);
                    Repwo("{Дата}", now.ToString("dd.MM.yyyy HH:mm"), doc);
                    Repwo("{Клиент}", $"{_user.Surname} {_user.Name} {_user.Patronymic}", doc);
                    Repwo("{Сумма}", CartTotal.ToString(), doc);
                    Repwo("{Скидка}", Discount.ToString(), doc);
                    Repwo("{К оплате}", Payment.ToString(), doc);

                    if (doc.Tables.Count > 0)
                    {
                        Word.Table table = doc.Tables[1];

                        for (int i = 0; i < Cart.Count; i++)
                        {
                            Word.Row row;

                            if (i == 0)
                            {
                                row = table.Rows[2];
                            }
                            else
                            {
                                row = table.Rows.Add();
                            }

                            row.Cells[1].Range.Text = (i + 1).ToString();
                            row.Cells[2].Range.Text = Cart[i].Good.Name;
                            row.Cells[3].Range.Text = Cart[i].Good.Price.ToString();
                            row.Cells[4].Range.Text = Cart[i].Quantitty.ToString();
                            row.Cells[5].Range.Text = (Cart[i].Good.Price * Cart[i].Quantitty).ToString();
                        }

                        if (Cart.Count == 0 && table.Rows.Count >= 2)
                        {
                            table.Rows[2].Delete();
                        }
                    }

                    doc.SaveAs2(outputPath);

                    isSuccessful = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при работе с Word: {ex.Message}");
                }
                finally
                {
                    if (doc != null)
                    {
                        try { doc.Close(Word.WdSaveOptions.wdDoNotSaveChanges); } catch { }
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(doc);
                    }
                    if (wordApp != null)
                    {
                        try { wordApp.Quit(); } catch { }
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
                    }
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                if (isSuccessful)
                {
                    SendEmailWithAttachment(outputPath, _user.Email);

                    MessageBox.Show("Чек сохранен в папке приложения и отправлен на почту");
                }
            }
            else
            {
                MessageBox.Show(error);
            }
        }

        private void Repwo(string subToReplace, string text, Word.Document worddoc)
        {
            var range = worddoc.Content;
            range.Find.ClearFormatting();
            range.Find.Execute(FindText: subToReplace, ReplaceWith: text);
        }

        private void SendEmailWithAttachment(string filePath, string recipientEmail)
        {
            string fromEmail = "unitytestluk@gmail.com";

            string appPassword = "kfrg nbmb nexa xljf";

            string smtpHost = "smtp.gmail.com";
            int smtpPort = 587;

            try
            {
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(fromEmail);
                mail.To.Add(recipientEmail);
                mail.Subject = "Ваш чек заказа";
                mail.Body = "Здравствуйте! Во вложении к этому письму находится ваш чек.";
                mail.IsBodyHtml = false;

                Attachment attachment = null;
                if (File.Exists(filePath))
                {
                    attachment = new Attachment(filePath);
                    mail.Attachments.Add(attachment);
                }
                else
                {
                    MessageBox.Show("Файл не найден");
                    return;
                }

                SmtpClient smtp = new SmtpClient(smtpHost, smtpPort);
                smtp.Credentials = new NetworkCredential(fromEmail, appPassword);
                smtp.EnableSsl = true;

                smtp.Send(mail);

                if (attachment != null) attachment.Dispose();

                mail.Dispose();
                smtp.Dispose();
            }
            catch (Exception ex)
            {
                string innerMessage = ex.InnerException != null ? ex.InnerException.Message : "нет деталей";

                MessageBox.Show($"Ошибка отправки: {ex.Message}\nДетали от сервера: {innerMessage}");
            }
        }

    }
}
