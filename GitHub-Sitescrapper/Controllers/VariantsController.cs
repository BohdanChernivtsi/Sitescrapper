using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using HtmlAgilityPack;
using System.Text;
using System.Data.SqlClient;
using System.Data;
//using System.Windows.Forms;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Sitescrapper.Controllers
{
    [ApiController]
    [Route("variants")]
    public class VariantsController : ControllerBase
    {
        private static Random rnd;
        static VariantsController()
        {
            rnd = new Random();
        }



        //GET: api/<VariantsController>
        [HttpGet]
        public ActionResult Get()
        //public ActionResult Get([FromBody] byte[] imageArr = null)
        {
            for (var i = 1; i <= 20; i++)
            {
                Log(i);

                var videoSearches = ParseHTML(i);

                foreach (var vs in videoSearches)
                {
                    ExecuteStoredProcedure(vs);
                }
            }
            

            return Ok();
        }

        //GET: api/<VariantsController>/SearchByTitle
        [HttpGet]
        [Route("search")]
        public ActionResult Search(
            [FromQuery] string title,
            [FromQuery] string year,
            [FromQuery] string genre,
            [FromQuery] string description,
            [FromQuery] string directedBy,
            [FromQuery] string actors,

            [FromQuery] int quantity,
            [FromQuery] string sort,
            [FromQuery] string sortBy)
        {
            // params quantity 1 - 10
            // order by title, year, genre, rating, duration
            var topQuantity = 10;
            if (quantity >= 1 && quantity <= 10)
            {
                topQuantity = quantity;
            }

            var sql = "SELECT TOP (" + topQuantity.ToString() + ")" +
                " [title] ,[year] ,[duration] ,[genre] ,[rating] ,[description] ,[directedBy] ,[actors]" +
                " from [VideoSearch].[dbo].[VideoSearches]" +
                " where title like '%" + title + "%'" +
                " AND [year] like '%" + year + "%'" +
                " AND [genre] like '%" + genre + "%'" +
                " AND [description] like '%" + description + "%'" +
                " AND [directedBy] like '%" + directedBy + "%'" +
                " AND [actors] like '%" + actors + "%'";

            if (!string.IsNullOrEmpty(sortBy))
            {
                var newSortBy = "title";
                switch(sortBy)
                {
                    case "title":
                        newSortBy = "title";
                        break;
                    case "year":
                        newSortBy = "year";
                        break;
                    case "genre":
                        newSortBy = "genre";
                        break;
                    case "rating":
                        newSortBy = "rating";
                        break;
                    case "duration":
                        newSortBy = "duration";
                        break;
                }

                var asc = "ASC";

                if (sort == "DESC")
                {
                    asc = "DESC";
                }

                sql += " ORDER BY " + newSortBy + " " + asc;
            }

            sql += " FOR JSON AUTO;";

            using (var conn = new SqlConnection("data source=.;database=VideoSearch;integrated security=SSPI"))
            using (var command = new SqlCommand(sql, conn))
            {
                

                conn.Open();
                
                var dataReader = command.ExecuteReader();

                object result = "";

                while (dataReader.Read())
                {
                    result += dataReader.GetValue(0).ToString();
                }

                return Ok(result);
            }
        }

        [HttpGet]
        [Route("search-like-title")]
        public ActionResult SearchLikeTitle(
            [FromQuery] string title,
            [FromQuery] string year,
            [FromQuery] string genre,
            [FromQuery] string description,
            [FromQuery] string directedBy,
            [FromQuery] string actors,

            [FromQuery] int quantity,
            [FromQuery] string sort,
            [FromQuery] string sortBy)
        {
            // params quantity 1 - 10
            // order by title, year, genre, rating, duration
            var topQuantity = 10;
            if (quantity >= 1 && quantity <= 10)
            {
                topQuantity = quantity;
            }

            var sql = "SELECT TOP (" + topQuantity.ToString() + ")" +
                " [title] ,[year] ,[duration] ,[genre] ,[rating] ,[description] ,[directedBy] ,[actors]" +
                " from [VideoSearch].[dbo].[VideoSearches]" +
                " where CHARINDEX(title, '"+ title + "') > 0" +
                " AND [year] like '%" + year + "%'" +
                " AND [genre] like '%" + genre + "%'" +
                " AND [description] like '%" + description + "%'" +
                " AND [directedBy] like '%" + directedBy + "%'" +
                " AND [actors] like '%" + actors + "%'";

            if (!string.IsNullOrEmpty(sortBy))
            {
                var newSortBy = "title";
                switch (sortBy)
                {
                    case "title":
                        newSortBy = "title";
                        break;
                    case "year":
                        newSortBy = "year";
                        break;
                    case "genre":
                        newSortBy = "genre";
                        break;
                    case "rating":
                        newSortBy = "rating";
                        break;
                    case "duration":
                        newSortBy = "duration";
                        break;
                }

                var asc = "ASC";

                if (sort == "DESC")
                {
                    asc = "DESC";
                }

                sql += " ORDER BY " + newSortBy + " " + asc;
            }

            sql += " FOR JSON AUTO;";

            using (var conn = new SqlConnection("data source=.;database=VideoSearch;integrated security=SSPI"))
            using (var command = new SqlCommand(sql, conn))
            {


                conn.Open();

                var dataReader = command.ExecuteReader();

                object result = "";

                while (dataReader.Read())
                {
                    result += dataReader.GetValue(0).ToString();
                }

                return Ok(result);
            }
        }

        //like title
        // random year, ...

        //private async void makeRequest()
        //{
        //    //var client = new HttpClient();
        //    //var request = new HttpRequestMessage
        //    //{
        //    //    Method = HttpMethod.Get,
        //    //    RequestUri = new Uri("https://www.imdb.com/search/keyword/_ajax?sort=moviemeter,asc&mode=detail&page=1&ref_=kw_ref_key"),
        //    //};
        //    //using (var response = await client.SendAsync(request))
        //    //{
        //    //    response.EnsureSuccessStatusCode();
        //    //    var body = await response.Content.ReadAsStringAsync();
        //    //    Console.WriteLine(body);
        //    //}
        //    ParseHTML(1);
        //}

        private IEnumerable<VideoSearch> ParseHTML(int pageNumber)
        {
            var url = $"secretSite&page={ pageNumber }&ref_=kw_ref_key";
            var web = new HtmlWeb();
            var doc = web.Load(url);

            var list = doc.DocumentNode
                .SelectNodes("//div[@class='lister-list']")
                .First();

            var listItems = list.SelectNodes("//div[@class='lister-item mode-detail']")
                .ToList();

            return listItems
                .Select(x => {
                    var imageSrc = x.Descendants().FirstOrDefault(x => x.Attributes.Any(x => x.Name == "class") && x.Attributes["class"].Value == "lister-item-image ribbonize")
                        .Descendants().Where(x => x.Name == "a").First()
                        .Descendants().Where(x => x.Name == "img").First().GetAttributeValue("src", null);
                    var content = x.Descendants().FirstOrDefault(x => x.Attributes.Any(x => x.Name == "class") && x.Attributes["class"].Value == "lister-item-content");
                    var header = content.Descendants().FirstOrDefault(x => x.Attributes.Any(x => x.Name == "class") && x.Attributes["class"].Value == "lister-item-header");
                    var textMuted = content.Descendants().FirstOrDefault(x => x.Attributes.Any(x => x.Name == "class") && x.Attributes["class"].Value == "text-muted text-small");

                    var title = header.Descendants("a").First().GetDirectInnerText();

                    var rawYear = header.Descendants().FirstOrDefault(x => x.Attributes.Any(x => x.Name == "class") && x.Attributes["class"].Value == "lister-item-year text-muted unbold").GetDirectInnerText();
                    var year = string.IsNullOrEmpty(rawYear) ? null : GetTruncatedString(rawYear);

                    var rawDuration = textMuted.Descendants().FirstOrDefault(x => x.Attributes.Any(x => x.Name == "class") && x.Attributes["class"].Value == "runtime");
                    var duration = rawDuration != null ? rawDuration.GetDirectInnerText() : null;

                    var rawGenre = textMuted.Descendants().FirstOrDefault(x => x.Attributes.Any(x => x.Name == "class") && x.Attributes["class"].Value == "genre");
                    var genre = rawGenre == null ? textMuted.Descendants().Where(x => x.Name == "b").FirstOrDefault()?.GetDirectInnerText() : rawGenre.GetDirectInnerText().Trim(new Char[] { ' ', '\n' });

                    var rawRating = content.Descendants().FirstOrDefault(x => x.Attributes.Any(x => x.Name == "class") && x.Attributes["class"].Value == "ratings-bar");
                    float? rating = null;
                    if (rawRating != null)
                    {
                        var result = rawRating.Descendants().FirstOrDefault(x => x.Attributes.Any(x => x.Name == "class") && x.Attributes["class"].Value == "inline-block ratings-imdb-rating");

                        if (result == null)
                        {
                            rating = null;
                        } else
                        {
                            var result2 = result.Descendants("strong").First().GetDirectInnerText();
                            
                            rating = GetRandomNumber(float.Parse(result2));
                        }
                    } else
                    {
                        rating = null;
                    }

                    var rawDescription = content.Descendants().FirstOrDefault(x => x.Attributes.Any(x => x.Name == "class") && x.Attributes["class"].Value == "").GetDirectInnerText();
                    var description = rawDescription.Trim('\n');

                    var cast = content.Descendants().Skip(14).FirstOrDefault(x => x.Attributes.Any(x => x.Name == "class") && x.Attributes["class"].Value == "text-muted text-small");
                    string directedBy;
                    string actors;
                    if (cast.Descendants().First().GetDirectInnerText().Trim(new char[] { ' ', '\n' }) == "Director:")
                    {
                        directedBy = cast.Descendants().Skip(1).First().GetDirectInnerText();
                        actors = cast.Descendants().Skip(2).Where(x => x.Name == "a").Aggregate("", (acc, x) => acc == "" ? acc + x.GetDirectInnerText() : acc + ", " + x.GetDirectInnerText());
                    }
                    else if (cast.Descendants().First().GetDirectInnerText().Trim(new char[] { ' ', '\n' }) == "Directors:")
                    {
                        directedBy = cast.Descendants().Skip(1).Where(x => x.Name == "a").Take(2).Aggregate("", (acc, x) => acc == "" ? acc + x.GetDirectInnerText() : acc + ", " + x.GetDirectInnerText());
                        actors = cast.Descendants().Skip(3).Where(x => x.Name == "a").Aggregate("", (acc, x) => acc == "" ? acc + x.GetDirectInnerText() : acc + ", " + x.GetDirectInnerText());
                    }
                    else
                    {
                        directedBy = null;
                        actors = cast.Descendants().Where(x => x.Name == "a").Aggregate("", (acc, x) => acc == "" ? acc + x.GetDirectInnerText() : acc + ", " + x.GetDirectInnerText());
                    }

                    return new VideoSearch() { title = title, year = year, duration = duration, genre = genre, rating = rating, description = description,
                                                directedBy = directedBy, actors = actors };
                });
            //lister - list // list
            //    lister - item mode - detail // item
            //        lister - item - image ribbonize
            //            a
            //                img[src] // image url]
            //        lister - item - content
            //            lister - item - header
            //                a[innerHtml] // title
            //                lister - item - year text - muted unbold [innerHTML] // year
            //            text - muted text - small
            //                runtime // duration
            //                genre // genre
            //                  
            //                  OR b innerText
            //
            //            ratings - bar
            //                inline - block ratings - imdb - rating
            //                    strong[innerHTML] //rating +-(1-3) random
            //            p(nth-child 4)[innerHtml]//description
            //            text - muted text - small
            //                a(nth-child 1)[innerHTML]// directed by
            //                a rest //actors
        }

        private void ExecuteStoredProcedure(VideoSearch vs)
        {
            using (var conn = new SqlConnection("data source=.;database=VideoSearch;integrated security=SSPI"))
            using (var command = new SqlCommand("AddVideoSearch", conn)
            {
                CommandType = CommandType.StoredProcedure
            })
            {
                setParameter(command, "@title", vs.title);
                setParameter(command, "@year", vs.year);
                setParameter(command, "@duration", vs.duration);
                setParameter(command, "@genre", vs.genre);
                setParameter(command, "@rating", vs.rating);
                setParameter(command, "@description", vs.description);
                setParameter(command, "@directedBy", vs.directedBy);
                setParameter(command, "@actors", vs.actors);

                conn.Open();
                command.ExecuteNonQuery();
            }
        }

        private async Task Log(int iteration)
        {
            await System.IO.File.WriteAllLinesAsync("LOGGER.txt", new string[] { iteration.ToString() });
        }

        private void setParameter(SqlCommand c, string name, object value = null)
        {
            if (value == null)
            {
                c.Parameters.AddWithValue(name, DBNull.Value);
            }
            else
            {
                c.Parameters.AddWithValue(name, value);
            }
        }

        public class VideoSearch
        {
            public string title;
            public string? year;
            public string? duration;
            public string genre;
            public float? rating;
            public string description;
            public string? directedBy;
            public string actors;
        }

        private string GetTruncatedString(string rawString)
        {
            char[] delimiterChars = { '(', ')' };

            var splitArr = rawString.Split(delimiterChars);

            var strBuilder = new StringBuilder();
            strBuilder.Append('(');
            strBuilder.Append(splitArr[splitArr.Length - 2]);
            strBuilder.Append(')');

            return strBuilder.ToString();
        }

        private float GetRandomNumber(float num)
        {
            var randomNumber = (rnd.NextDouble() * 5) - 2.5;
            var result = num + randomNumber;
            if (result <= 0 || result > 9.2)
            {
                return num;
            }
            var trimmed = Math.Round(randomNumber, 1);
            return float.Parse((num + trimmed).ToString("N2"));
        }

        //        // POST: api/<VariantsController>
        //        [HttpPost]
        //        public string GetFromImage([FromBody] byte[] imageArr = null)
        //        {


        //            //Google API key
        //            //AIzaSyBY9sa_K7aIJromCffCgL7BK938ksQcvL8
        //            var res = await GetReverseAPIData();

        //            return res;
        //        }

        //        private async Task<string> GetReverseAPIData(byte[] imageArr = null)
        //        {

        //            var values = new Dictionary<string, string>
        //                {
        //                    { "encoded_image", "hello" },
        //                    { "thing2", "world" }
        //                };

        //            var content = new FormUrlEncodedContent(values);

        //            string apiKey = "AIzaSyBY9sa_K7aIJromCffCgL7BK938ksQcvL8";

        //            var response = await _httpClient.GetAsync("http://www.googleapis.com/customsearch/v1?key=" + apiKey + "&q=business");

        //           return await response.Content.ReadAsStringAsync();

        ////            filePath = '/mnt/Images/test.png'
        ////searchUrl = 'http://www.google.hr/searchbyimage/upload'
        ////multipart = { 'encoded_image': (filePath, open(filePath, 'rb')), 'image_content': ''}
        ////            response = requests.post(searchUrl, files = multipart, allow_redirects = False)
        ////fetchUrl = response.headers['Location']
        ////webbrowser.open(fetchUrl)
        //        }
    }
}
