using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;

namespace gentleman
{
    public class FClient
    {
        private System.Net.WebClient client = null;
        public string secret = null;
        private int BlukSize = 20;

        public FClient()
        {            
            client = new System.Net.WebClient();
            client.Encoding = Encoding.UTF8;
        }

        private List<string> Submit(string url, NameValueCollection require, List<KeyValuePair<string, string>> datas)
        {                        
            var sentvalues = new NameValueCollection();
            List<string> Results = new List<string>();
                        
            for(int i =0;i<datas.Count; i++) 
            {
                KeyValuePair<string, string> data = datas[i];
                sentvalues.Add(data.Key, data.Value);                
                
                if (i % BlukSize == 0 || i == datas.Count -1)
                {
                    if(require != null)
                        sentvalues.Add(require);

                    sentvalues.Add("secret", secret);
                    System.IO.StreamWriter writer = new System.IO.StreamWriter(string.Format("{0}.txt", i));
                    var response = client.UploadValues(url, sentvalues);
                    var resstring = Encoding.UTF8.GetString(response);
                    writer.Write(resstring);
                    writer.Close();
                    foreach (var res in resstring.Split(','))
                        Results.Add(res);
                    
                    sentvalues.Clear();                    
                }
            }
            return Results;
        }

        public void UploadImage(Dictionary<string, List<string>> items)
        {
            List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();
            foreach (var item in items)
            {
                values.Add(new KeyValuePair<string, string>(
                    "data", string.Format("{0}:{1}", item.Key, string.Join(";", Algorithm.RemoveRepeat<string>(item.Value).ToArray()))));
            }
            Submit("http://pheever.appspot.com/UploadImage/", null, values);

        }

        public Dictionary<String, List<string>> QueryImage(List<string> ihashs)
        {            
            List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();            
            Dictionary<string, List<String>> Results = new Dictionary<string, List<string>>();
            
            foreach(string hash in ihashs)
            {
                values.Add(new KeyValuePair<string, string> ("hash", hash));                
            }

            var response = Submit("http://pheever.appspot.com/QueryImage/", null, values);

            for (int i = 0; i < ihashs.Count; i++)
            {
                Results[ihashs[i]] = (new List<string>(response[i].Split(';'))).FindAll(a=>a.Trim().Length > 0);
            }
            return Results;
        }

        public Dictionary<string, List<string>> QueryUserImage(List<String> ihashs)
        {
            List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>();
            Dictionary<string, List<String>> Results = new Dictionary<string, List<string>>();

            foreach (string hash in ihashs)
            {
                values.Add(new KeyValuePair<string, string>("hash", hash));
            }

            var response = Submit("http://pheever.appspot.com/QueryUserImage/", null, values);

            for (int i = 0; i < ihashs.Count; i++)
            {
                Results[ihashs[i]] = new List<string>(response[i].Split(';'));
            }
            return Results;
        }        
    }
}
