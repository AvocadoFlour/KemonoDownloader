
using HtmlAgilityPack;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;

const string pagination = "?o=";
Console.WriteLine("Input (paste or manually type in) the artist URL. The program will just shut off if you input something invalid. Already existing files will not be downloaded anew.");
var artistUrl = Console.ReadLine();

HtmlWeb hw = new HtmlWeb();

var pages = GetAllPages(artistUrl);
var fullPages = pages / 25;

for (int i = 0; i <= fullPages; i++)
{
    var postIds = GetAllPostOnAPage(artistUrl + pagination + i * 25);
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
    doc = hw.Load(postUrl);
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
                var fileName = postUrl.Split("post/")[1] + "_" + counter + "." + extension;
                if (!CheckIfFileExists(fileName))
                {
                    Console.WriteLine($"Saving: {fileName}");
                    if (extension.Equals("gif"))
                    {
                        SaveGif("https://kemono.party/" + hrefValue, fileName);
                    }
                    else SaveImage("https://kemono.party/" + hrefValue, fileName);

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
    var doc = hw.Load(postUrl);
    if (doc.DocumentNode.SelectNodes("//a[contains(@class, 'post__attachment-link')]") != null)
    {
        foreach (HtmlNode attachment in doc.DocumentNode.SelectNodes("//a[contains(@class, 'post__attachment-link')]"))
        {
            var url = attachment.GetAttributeValue("href", string.Empty);
            var fileName = attachment.InnerText;
            fileName = postUrl.Split("post/")[1] + "_" + fileName.Split("\n")[1].TrimStart().Split("\n")[0];
            if (!CheckIfFileExists(fileName))
            {
                WebClient webClient = new WebClient();
                Console.WriteLine($"Downloading attachment: {fileName}");
                webClient.DownloadFile(new Uri("https://kemono.party/" + url), fileName);
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
    Stream stream = client.OpenRead(imageUrl);
    Bitmap bitmap; bitmap = new Bitmap(stream);

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
    WebClient webClient = new WebClient();
    Console.WriteLine($"Downloading attachment: {fileName}");
    webClient.DownloadFile(new Uri(gifUrl), fileName);
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
