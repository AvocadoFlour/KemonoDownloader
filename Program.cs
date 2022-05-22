
using HtmlAgilityPack;
using System.Drawing;
using System.Net;
using System.Text.RegularExpressions;

const string pagination = "?o=";
const string kemonoBaseUrl = "https://kemono.party/";

Console.WriteLine("Input (paste or manually type in) the artist URL. The program will just shut off if you input something invalid. Already existing files will not be downloaded anew.");
var artistUrl = Console.ReadLine();

HtmlWeb hw = new HtmlWeb();


var pages = TryLoop(() =>
    {
        return GetAllPages(artistUrl);
    }
);

var fullPages = pages / 25;

if(pages%25 == 0)
{
    fullPages -= 1;
}

for (int i = 0; i <= fullPages; i++)
{
    var postIds = TryLoop(() =>
    {
        return GetAllPostOnAPage(artistUrl + pagination + i * 25);
    });

    foreach (string postId in postIds)
    {
        Sleep(1);
        
        GetPostAttachments(artistUrl + "/post/" + postId);
        GetImagesFromASinglePost(artistUrl + "/post/" + postId);
    }
}

int GetAllPages(string artistUrl)
{
    HtmlDocument doc = new HtmlDocument();
    doc = hw.Load(artistUrl);
    var paginator = doc.DocumentNode.SelectSingleNode("//small");
    return int.Parse(paginator.InnerHtml.Split("\n")[1].Split(" ").Last());
}


// get all posts of an artist on one page
List<string> GetAllPostOnAPage(string pageUrl)
{
    List<string> postIds = new List<string>();
    var doc = hw.Load(pageUrl);
    foreach (HtmlNode div in doc.DocumentNode.SelectNodes("//article[contains(@class, 'post-card')]"))
    {
        // Get the values of the href attribute of each post on the page
        string postId = div.GetAttributeValue("data-id", string.Empty);
        postIds.Add(postId);
    }
    return postIds;
}

void GetImagesFromASinglePost(string postUrl)
{
    HtmlDocument doc = new HtmlDocument();
    doc = TryLoop(() =>
    {
        return hw.Load(postUrl);
    });
    if (doc.DocumentNode.SelectNodes("//div[contains(@class, 'post__files')]") != null)
    {
        foreach (HtmlNode div in doc.DocumentNode.SelectNodes("//div[contains(@class, 'post__files')]"))
        {
            var counter = 0;
            // Get the value of the HREF attribute
            foreach (HtmlNode url in div.SelectNodes("//a[contains(@class, 'fileThumb')]"))
            {
                string hrefValue = url.GetAttributeValue("href", string.Empty);
                string extension = hrefValue.Split(".").Last();
                if (extension.Equals("jpe"))
                {
                    extension = "jpg";
                }
                var fileName = ValidateFileName(postUrl.Split("post/")[1] + "_" + counter + "." + extension);
                if (!CheckIfFileExists(fileName))
                {
                    Console.WriteLine($"Saving: {fileName}");
                    if (extension.Equals("gif"))
                    {
                        SaveGif(kemonoBaseUrl + hrefValue, fileName);
                    }
                    else SaveImage(kemonoBaseUrl + hrefValue, fileName);

                    Sleep();
                }
                else Console.WriteLine($"File exists, skipping: {fileName}");
                counter += 1;
            }
        }
    }
}

void GetPostAttachments(string postUrl)
{
    var doc = TryLoop(() =>
    {
        return hw.Load(postUrl);
    });

    if (doc.DocumentNode.SelectNodes("//a[contains(@class, 'post__attachment-link')]") != null)
    {
        foreach (HtmlNode attachment in doc.DocumentNode.SelectNodes("//a[contains(@class, 'post__attachment-link')]"))
        {
            var url = attachment.GetAttributeValue("href", string.Empty);
            var fileName = attachment.InnerText;
            fileName = ValidateFileName(postUrl.Split("post/")[1] + "_" + fileName.Split("\n")[1].TrimStart().Split("\n")[0]);
            if (!CheckIfFileExists(fileName))
            {
                var fullUrl = kemonoBaseUrl + url;
                Console.WriteLine("Downloading: " + fullUrl);
                WebClient webClient = new WebClient();
                Console.WriteLine($"Downloading attachment: {fileName}");
                TryLoopAction(() =>
                {
                    webClient.DownloadFile(new Uri(fullUrl), fileName);
                });
                Console.WriteLine("Download done.");
                webClient.Dispose();
                Sleep();
            }
            else Console.WriteLine($"File exists, skipping: {fileName}");
        }
    }
}

void SaveImage(string imageUrl, string filename)
{
    WebClient client = new WebClient();

    Console.WriteLine("Downloading: " + imageUrl);
    Stream stream = TryLoop(() =>
    {
        return client.OpenRead(imageUrl);
    });
    Bitmap bitmap = null;
    TryLoopAction(() =>
    {
        bitmap = new Bitmap(stream);
    });

    if (bitmap != null)
    {
        bitmap.Save(filename);
    }

    stream.Flush();
    stream.Close();
    client.Dispose();
}

void SaveGif(string gifUrl, string fileName)
{
    Console.WriteLine("Downloading: " + gifUrl);
    WebClient webClient = new WebClient();
    Console.WriteLine($"Downloading attachment: {fileName}");
    TryLoopAction(() =>
    {
        webClient.DownloadFile(new Uri(gifUrl), fileName);
    });
    webClient.Dispose();
}

bool CheckIfFileExists(string fileName)
{
    var workingDirectory = Environment.CurrentDirectory;
    var file = $"{workingDirectory}\\{fileName}";
    return File.Exists(file);
}

void Sleep(int length = 1)
{
    Random rnd = new Random();
    var randInt = 0;
    if (length == 0)
    {
        randInt = rnd.Next(500, 3000);
        Console.WriteLine($"Next post, slept for {randInt} miliseconds so as not to overburden the site.");
    }
    else
    {
        randInt = rnd.Next(214, 958);
    }

    Thread.Sleep(randInt);
}

string ValidateFileName(string input, string replacement = "")
{
    var regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
    var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
    var fileName = r.Replace(input, replacement);
    if (fileName.Length > 120)
    {
        fileName = fileName.Substring(0, 119);
    }
    return fileName;
}

// https://stackoverflow.com/a/23103561/10299831
T TryLoop<T>(Func<T> anyMethod)
{
    while (true)
    {
        try
        {
            return anyMethod();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            System.Threading.Thread.Sleep(2000); // *
        }
    }
    return default(T);
}

void TryLoopAction(Action anyAction)
{
    while (true)
    {
        try
        {
            anyAction();
            break;
        }
        catch(Exception e)
        {
            Console.WriteLine(e.Message);
            System.Threading.Thread.Sleep(2000); // *
        }
    }
}