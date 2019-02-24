
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using Toxy;
using System.Text.RegularExpressions;  //正規表現：Regexを利用する

using Newtonsoft.Json;

using System.Net;
using System.Net.Http;

// https://qiita.com/mash0510/items/caa41b1f1d8dc4b31ac6


namespace PDF1
{
    class Program
    {
        static void Main()
        {

            string PostJson="";


            ParserContext context = new ParserContext(@"D:\SelfStudy\PDF1\FAQ_Short.pdf");
            ITextParser extractParser = ParserFactory.CreateText(context);
            string extractedText = extractParser.Parse();


            //改行を削除　 http://www.atmarkit.co.jp/ait/articles/1004/08/news094.html
            extractedText = extractedText.Replace("\r", "").Replace("\n", "");
               

            //　., ? を基準に改行
            extractedText = extractedText.Replace(".", ".\r\n");
            extractedText = extractedText.Replace("?", "?\r\n");

            //空白行を削除(.文字だけの行）　http://baba-s.hatenablog.com/entry/2018/05/18/171500
            extractedText = Regex.Replace
            (
                extractedText,
                "^.[\r\n]+",
                string.Empty,
                RegexOptions.Multiline
            );
                       
            //text書き出し: テスト時無効に  

            /***
            StreamWriter sw_out = new StreamWriter(@"D:\SelfStudy\PDF1\UFR.txt", true, Encoding.GetEncoding("shift_jis"));
            sw_out.Write(extractedText);
            sw_out.Close();
            ***/



            //JsonFile作成           
           MakeJSON(ref PostJson);  //参照渡し：https://dobon.net/vb/dotnet/beginner/byvalbyref.html
            MakeRequest(PostJson);            
        }

        
        static void  MakeJSON(ref string PostJson)
        {

            int counter = 1;
            string line;

          //textファイルの読み込み
          StreamReader sr = new StreamReader(@"D:\SelfStudy\PDF1\UFR.txt", Encoding.GetEncoding("shift_jis"));

          // string Read_text = sr.ReadToEnd();
          // sr.Close();


         //ルートオブジェクトをインスタンス化
         RootObject RO = new RootObject()
            {
                documents = new List<documents>()
            };


            // テキスト内容をJSONファイル化

            //documents userdata = new documents();

            while((line = sr.ReadLine())!= null){
                documents userdata = new documents();
                userdata.language = "en";
                userdata.id = Convert.ToString(counter);  //https://www.sejuku.net/blog/44977
                userdata.text = line;

                counter++;
                RO.documents.Add(userdata);
            }

            PostJson = JsonConvert.SerializeObject(RO);
                    
           sr.Close();

           // return PostJson;
         }
        


        //Make Request 

        static void  MakeRequest(string PostJson)
        {
            var uri = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/keyPhrases";
                     

            HttpWebRequest Wreq = (HttpWebRequest)HttpWebRequest.Create(uri);
            Wreq.Headers.Add("Ocp-Apim-Subscription-Key:7cfd2ff5b5f64ea08edd6f5b37793a06");
            Wreq.Method = "POST";

            //文字コードをUTF-8に変換
            byte[] BODY = Encoding.UTF8.GetBytes(PostJson);
            Wreq.ContentLength = BODY.Length;  //BODYの長さを設定

            //送信するBODYをWebReqestにセットする

            Stream Wstream = Wreq.GetRequestStream();
            Wstream.Write(BODY, 0, BODY.Length);

            //サーバのリクエスト送信
            HttpWebResponse Wres = (HttpWebResponse)Wreq.GetResponse();



            //受け取ったResponse：(バイナリ）から文字列として読み出す。System.IO を利用する

            Stream WresStream = Wres.GetResponseStream();
            StreamReader WSReader = new StreamReader(WresStream);
            string result = WSReader.ReadToEnd();



            //デシリアライズでデータを取り出そう
            Re_RootObject re_RO = JsonConvert.DeserializeObject<Re_RootObject>(result);

            
            //IDとKeyPhaseを出力
            foreach(Re_Document re_Do in re_RO.documents)
            {
                Console.WriteLine(re_Do.id);
                foreach (string keys in re_Do.keyPhrases) 
                {
                    Console.WriteLine(" {0} ",keys);
                }
                
               
            }

            Console.ReadKey();

        }



    }


    //JSON用
    public class documents//dcoumentsの雛形、構造定義
    {
        public string language { get; set; }
        public string id { get; set; }
        public string text { get; set; }

    }

    public class RootObject //ルートオブジェクト
    {
        public List<documents> documents { get; set; }

    }




    public class Re_Document
    {
        public string id { get; set; }
        public List<string> keyPhrases { get; set; }
    }

    public class Re_RootObject
    {
        public List<Re_Document> documents { get; set; }
        public List<object> errors { get; set; }
    }




}

