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
                    client.login("邮箱", "密码");
                    //获取邮箱文件夹
                    var list = client.FoldersList();
                    //获取指定文件夹
                    var hasFolder = client.SelectFolder("INBOX");
                    if (hasFolder)
                    {
                        //要查询的时间
                        var ids = client.Search(DateTime.Now.AddDays(-1));
                        if (ids != null)
                        {
                            foreach (var id in ids)
                            {
                                try
                                {
                                    var ID = Convert.ToInt32(id);
                                    //获取邮件头
                                    var header = client.GetHeader(ID);
                                    //获取邮件主体
                                    var body = client.GetBody(ID);
                                    //获取附件
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
