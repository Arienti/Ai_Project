using Ai_Project.DTO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Ai_Project.Repository
{
    public class Api
    {

        public static async Task<ResponseDTO?> HTTPPost(string prompt)
        {
            RequestDTO requestDTO = new RequestDTO
            {
                prompt = prompt,
                model = "phi:latest",
                stream = false
            };

            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(2);
            string restfulApi = "http://localhost:11434/api/generate";

            StringContent requestContent = new StringContent(JsonConvert.SerializeObject(requestDTO), Encoding.UTF8, "application/json");

            try
            {
                using HttpResponseMessage response = await client.PostAsync(restfulApi, requestContent);

                var jsonResponse = await response.Content.ReadAsStringAsync();

                ResponseDTO? result = JsonConvert.DeserializeObject<ResponseDTO>(jsonResponse);
                //if ((result == null) || (result.Data == null))
                //{
                //    return ResultDTO.Fail("can't find object with this restful API");
                //}
                //if (!string.IsNullOrEmpty(result.Data.ToString()))
                //{
                //    string? checkData = result.Data.ToString()?.Replace("[]", "");
                //    if (string.IsNullOrEmpty(checkData))
                //    {
                //        return ResultDTO.Fail("can't find object with this restful API");
                //    }
                //}
                return result;
            }
            catch
            {
                return new ResponseDTO();
                //return ResultDTO.Fail("can't find object with this restful API");
            }
        }
    }
}
