大家使用C#同步邮件的时候，会不会遇到一件头疼的事情，C#只支持POP协议（只能获取收件箱的邮件，其他文件夹获取不到），Imap需要用第三方的来，而常见的几个第三方的类库，在同步大附件的时候，总是会超时。迫于无奈，我只好自己写一个Imap协议获取邮件。通过不断调试，获取大附件再也不会超时了，下面就跟大家分享一下。

[查看原文](https://www.codernice.top/articles/916a8cf.html)

![](https://codernice.coding.me/pictureurl/codernice/imaphelper.jpg)

# 准备知识

程序原理就是利用TCP请求邮件服务器，然后发送IMAP命令执行一系列操作。
## 1.登录
`a01 xxx@126.com password`
结果：`a01 OK LOGIN completed`
## 2.获取文件夹列表
`a02 list "" *`
结果：
```
LIST () “/” “INBOX”
LIST (\Drafts) “/” “&g0l6P3ux-”
LIST (\Sent) “/” “&XfJT0ZAB-”
LIST (\Trash) “/” “&XfJSIJZk-”
LIST (\Junk) “/” “&V4NXPpCuTvY-”
LIST () “/” “&dcVr0pCuTvY-”
LIST () “/” “&Xn9USpCuTvY-”
LIST () “/” “&i6KWBZCuTvY-”
LIST () “/” “&YhF2hGWHaGM-”
LIST () “/” “java”
LIST () “/” “&bUuL1WWHTvZZOQ-”
LIST () “/” “&Y6hef5CuTvY-”
LIST () “/” “test”
LIST () “/” “folder”
a02 OK LIST Completed
```
## 3.选择某个文件夹
`a03 select inbox`
结果：
```
2918 EXISTS
458 RECENT
OK [UIDVALIDITY 1] UIDs valid
FLAGS (\Answered \Seen \Deleted \Draft \Flagged)
OK [PERMANENTFLAGS (\Answered \Seen \Deleted \Draft \Flagged)] Limited
a03 OK [READ-WRITE] SELECT completed
```
## 4.获取日期开始邮件
`a04 SEARCH SINCE 日-月-年`
其中月为英文缩写，如Jan，Feb
## 5.获取邮件内容
```
b01 fetch body[header]  //邮件头
b01 fetch body[1]       //邮件主体
```
具体命令大家可自行百度学习，这边就不过多讲解。

# 代码详解
## 连接
```
public void Connection(string serveraddress, int port)
{
    try
    {
        tcpclient.Connect(serveraddress, port);
        sslstream = new SslStream(tcpclient.GetStream());
        sslstream.AuthenticateAsClient(serveraddress);
        bool flag = sslstream.IsAuthenticated;
        if (!flag)
        {
            throw new Exception("sslstream IsAuthenticated return false");
        }
    }
    catch (Exception ex)
    {
        throw ex;
    }
}
```
## 登录
构造imap命令，使用StreamWriter写入命令，StreamReader逐行获取，判断失败返回的关键字
```
public void login(string username, string password)
{
    sw = new StreamWriter(sslstream);
    // Assigned reader to stream
    reader = new StreamReader(sslstream);
    sw.WriteLine("a01 LOGIN " + username + " " + password);
    sw.Flush();
    try
    {
        string strTemp = string.Empty;
        while ((strTemp = reader.ReadLine()) != null)
        {
            if (strTemp.IndexOf("OK LOGIN completed") != -1)
            {
                break;
            }
            else if (strTemp.IndexOf("NO LOGIN failed") != -1)
            {
                throw new Exception("NO LOGIN failed");
            }
        }
    }
    catch (Exception ex)
    {
        throw ex;
    }
}
```
## 查看文件夹
同理提交查看文件夹的命令，循环读取返回内容，直到"OK LIST completed"结束
```
public string FoldersList()
{
    sw.WriteLine("a02 list \"\" *");
    sw.Flush();
    string str = string.Empty;
    try
    {
        string strTemp = string.Empty;
        while ((strTemp = reader.ReadLine()) != null)
        {
            if (strTemp.IndexOf("OK LIST completed") != -1)
            {
                break;
            }
            str += strTemp + "\r";
        }
    }
    catch (Exception ex)
    {
        return "error:" + ex.Message;
    }
    return str;
}
```
## 选择文件夹
存在"SELECT completed"返回True，否则False
```
public bool SelectFolder(string folder)
{
    sw.WriteLine("a03 select " + folder);
    sw.Flush();
    try
    {
        string strTemp = string.Empty;
        while ((strTemp = reader.ReadLine()) != null)
        {
            if (strTemp.IndexOf("NO SELECT failed") != -1)
            {
                return false;
            }
            if (strTemp.IndexOf("SELECT completed") != -1)
            {
                return true;
            }
        }
    }
    catch (Exception ex)
    {
        return false;
    }
    return false;
}
```
## 获取邮件id
使用"SEARCH SINCE 日期"命令获取某天开始的邮件，ToSearchDt()方法会将日期转成IMAP命令需要的格式，返回的是uid数组
```
public string[] Search(DateTime dateTime, bool isReverse = true)
{
    string strDateTime = ToSearchDt(dateTime);
    sw.WriteLine("a04 SEARCH SINCE " + strDateTime);
    sw.Flush();
    string str = string.Empty;
    try
    {
        string strTemp = string.Empty;
        while ((strTemp = reader.ReadLine()) != null)
        {
            if (strTemp.IndexOf("SEARCH") != -1)
            {
                str = strTemp.Replace("SEARCH", "").Replace("*", "").Trim();
            }
            break;
        }
    }
    catch (Exception ex)
    {
        ;
    }
    if (string.IsNullOrEmpty(str)) return null;
    string[] arr = System.Text.RegularExpressions.Regex.Split(str, @"\s+");
    if (isReverse)
    {
        arr = arr.Reverse().ToArray();
    }
    return arr;
}
```
## 获取邮件
这边主要是做了一个判断是否附件的处理，如果是附件设置一次读取的大小，再循环读取。如果用行读取的话，附件太大的话会卡很久到超时。这边设置的是一次100K，这个值可以适当调整
```
private string fetch(int id, string data, bool line = false)
{
    sw.WriteLine("b01 fetch " + id + " " + data);
    sw.Flush();
    string str = string.Empty;
    try
    {
        System.Text.RegularExpressions.Regex reg1 = new System.Text.RegularExpressions.Regex(@"body\[[1-9][0-9]{0,1}\]");
        bool ismatch = reg1.IsMatch(data);
        if (ismatch && data != "body[1]")
        {
            //100k
            var clen = 1024 * 100;
            var read = new Char[clen];
            var count = reader.Read(read, 0, clen);
            while (count > 0)
            {
                var strTemp = new string(read, 0, count);
                str += strTemp;
                if (strTemp.IndexOf("OK FETCH completed") != -1)
                    break;
                count = reader.Read(read, 0, clen);
            }
            var arr = str.Split('\r');
            string[] b = new string[arr.Length - 3];
            Array.Copy(arr, 1, b, 0, arr.Length - 3);
            str = string.Join("\r", b).Replace("\r\n", "").Replace(")", "");
        }
        else
        {
            string strTemp = string.Empty;
            while ((strTemp = reader.ReadLine()) != null)
            {
                if (strTemp.IndexOf("OK SEARCH completed") != -1 || strTemp.ToLower().IndexOf(data) != -1)
                {
                    continue;
                }
                if (strTemp.IndexOf("OK FETCH completed") != -1 || strTemp.IndexOf("BAD invalid command or parameters") != -1)
                {
                    break;
                }
                if (line)
                {
                    str += strTemp;
                }
                else
                {
                    str += strTemp + "\r";
                }
            }
        }
    }
    catch (Exception ex)
    {
        ;
    }
    return str;
}
```
这边主要讲解核心的方法，其余的是一些解析邮件内容的方法，大家可下载源码自行研究。

为何需要解析？因为获取下来的是一大串的文本，我们需要根据一些标识，获取到我们想要的东西，如发件时间、发送的邮箱、标题、主题和附件等。我解析的方法可能略显粗糙，希望大家有兴趣的话可以来优化一下。

## 调用实例
这边举例获取收件箱的邮件，如果全部文件夹都需要则可以遍历list，
```
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
```
源代码：[https://github.com/codernice/ImapHelper.git](https://github.com/codernice/ImapHelper.git)