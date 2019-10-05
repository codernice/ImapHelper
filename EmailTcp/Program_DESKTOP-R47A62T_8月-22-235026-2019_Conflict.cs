using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net.Security;
using System.IO;
using ImapHelper;
using System.Drawing;
using System.Drawing.Imaging;

namespace EmailTcp
{
    class Program
    {
        static void Main(string[] args)
        {
            using (ImapClient client = new ImapClient())
            {
                try
                {
                    client.Connection("imap.mxhichina.com", 993);
                    client.login("jenny@holystone.com", "Hs140621");
                    //var list = client.FoldersList();
                    var hasFolder = client.SelectFolder("inbox");
                    if (hasFolder)
                    {
                        var ids = client.Search(DateTime.Now.AddDays(-1));
                        if (ids != null)
                        {
                            foreach (var id in ids)
                            {
                                try
                                {
                                    var ID = Convert.ToInt32(id);
                                    var header = client.GetHeader(ID);
                                    var body = client.GetBody(ID);
                                    var attachemnts = client.GetAttachments(ID);
                                    foreach (var attachment in attachemnts)
                                    {
                                        byte[] bytes = Convert.FromBase64String(attachment.bs64);
                                        string uploadDir = "img";
                                        if (!Directory.Exists(uploadDir))
                                        {
                                            Directory.CreateDirectory(uploadDir);
                                        }
                                        using (Image img = Image.FromStream(new MemoryStream(bytes)))
                                        {
                                            var fileName = uploadDir += "/" + System.Guid.NewGuid().ToString() + ".jpg";
                                            img.Save(uploadDir, ImageFormat.Jpeg);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("获取邮件异常，id:{0},异常{1}", id, ex.Message);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ;
                }
            }
        }
    }
}
