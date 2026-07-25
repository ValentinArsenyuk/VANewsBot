using System.ServiceModel.Syndication;
using System.Xml;
using TelegramBot.Models;

namespace TelegramBot.Services
{
    public class NewsRiskService
    {
        private readonly string rssUrl =
            "https://www.timesofisrael.com/feed/";


        public async Task<RiskResult> CalculateRisk()
        {
            var result = new RiskResult();


            var keywords =
                new Dictionary<string, (int Score, string Reason)>
            {
                {
                    "missile launch",
                    (40, "🚀 Запуск ракет")
                },

                {
                    "missile",
                    (25, "🚀 Ракетная угроза")
                },

                {
                    "rocket",
                    (25, "🚀 Сообщения о ракетах")
                },

                {
                    "airstrike",
                    (35, "✈️ Авиационный удар")
                },

                {
                    "strike",
                    (20, "💥 Военный удар")
                },

                {
                    "attack",
                    (15, "⚠️ Сообщение об атаке")
                },

                {
                    "retaliation",
                    (20, "⚔️ Угроза ответного удара")
                },

                {
                    "nuclear",
                    (15, "☢️ Ядерная тема")
                },

                {
                    "irgc",
                    (10, "🎖 Активность КСИР")
                },

                {
                    "war",
                    (15, "⚔️ Упоминание войны")
                }
            };



            using var client = new HttpClient();


            string xml;

            try
            {
                xml =
                    await client.GetStringAsync(
                        rssUrl
                    );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Ошибка RSS: " + ex.Message
                );

                return result;
            }



            using var reader =
                XmlReader.Create(
                    new StringReader(xml)
                );


            var feed =
                SyndicationFeed.Load(reader);



            int totalScore = 0;

            int newsCount = 0;



            foreach (var item in feed.Items.Take(10))
            {
                newsCount++;


                string title =
                    item.Title?.Text ?? "";


                string summary =
                    item.Summary?.Text ?? "";


                string text =
                    (
                    title +
                    " " +
                    summary
                    )
                    .ToLower();



                int newsScore = 0;



                foreach (var word in keywords)
                {
                    if (text.Contains(
                        word.Key.ToLower()))
                    {

                        newsScore +=
                            word.Value.Score;


                        if (!result.Reasons.Contains(
                            word.Value.Reason))
                        {
                            result.Reasons.Add(
                                word.Value.Reason
                            );
                        }
                    }
                }



                // сохраняем только важные новости

                if (newsScore >= 20)
                {
                    result.NewsTitles.Add(title);
                }



                // одна новость максимум 40

                if (newsScore > 40)
                {
                    newsScore = 40;
                }


                totalScore += newsScore;
            }



            if (newsCount > 0)
            {
                double average =
                    (double)totalScore /
                    newsCount;


                /*
                   Усилитель риска.
                   Чтобы важные события
                   не терялись среди обычных новостей.
                */

                result.Score =
                    (int)(average * 1.8);
            }



            // максимум 100

            if (result.Score > 100)
            {
                result.Score = 100;
            }



            // если есть подозрительные новости,
            // минимальный риск 5%

            if (result.Score < 5 &&
               result.Reasons.Count > 0)
            {
                result.Score = 5;
            }



            Console.WriteLine(
                $"Новости: {newsCount}"
            );


            Console.WriteLine(
                $"Сумма баллов: {totalScore}"
            );


            Console.WriteLine(
                $"Итоговый риск: {result.Score}%"
            );



            return result;
        }
    }
}