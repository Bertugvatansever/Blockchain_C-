using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class Program
{
    static List<string> hashList = new List<string>();
    static string contractAddress = "0xFFc436B331559aa27FBD23E112aeb3F561CfdeB2";

    static async Task Main()
    {
        string apiKey = "UCDWJK542CSXBRGZF8SXSDS5RD4EYG6KCM";
        string address = "0xE5ed40436Fd514028403D5b310Ec0CFCa732502D";
        await ListTransactions(apiKey, address);
        await GetTransactionLogs(apiKey, hashList);
    }

    static async Task ListTransactions(string apiKey, string address)
    {
        using (HttpClient client = new HttpClient())
        {
            string apiUrl = $"https://api-sepolia.etherscan.io/api?module=account&action=txlist&address={address}&apikey={apiKey}";

            try
            {
                HttpResponseMessage response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResult = await response.Content.ReadAsStringAsync();
                    var parsedResult = JToken.Parse(jsonResult);

                    var transactions = parsedResult["result"];

                    if (transactions != null && transactions.HasValues)
                    {
                        Console.WriteLine("Kontrat İşlemleri:");

                        foreach (var transaction in transactions)
                        {
                            var tempAddress = transaction["to"];

                            // Eğer işlemi yapan adres belirli bir adrese eşleşiyorsa işlemi ekrana yazdır
                            if (tempAddress != null && tempAddress.ToString() == contractAddress.ToLower())
                            {
                                var transactionHash = transaction["hash"];
                                var blockNumber = transaction["blockNumber"];
                                hashList.Add(transactionHash.ToString());

                                // İşlem detaylarını içeren linki oluştur
                                var transactionLink = $"https://sepolia.etherscan.io/tx/{transactionHash}";

                                // Linki ekrana yazdır
                                Console.WriteLine($"- Block: {blockNumber}, Transaction Link: {transactionLink}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Hata: Kullanıcının işlem geçmişi bulunamadı.");
                    }
                }
                else
                {
                    Console.WriteLine($"Hata: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
            }
        }
    }



    static async Task GetTransactionLogs(string apiKey, List<string> transactionHashes)
    {
        foreach (var transactionHash in transactionHashes)
        {
            using (HttpClient client = new HttpClient())
            {
                string apiUrl = $"https://api-sepolia.etherscan.io/api?module=proxy&action=eth_getTransactionReceipt&txhash={transactionHash}&apikey={apiKey}";

                try
                {
                    HttpResponseMessage response = await client.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResult = await response.Content.ReadAsStringAsync();
                        var parsedResult = JToken.Parse(jsonResult);

                        // "result" kısmında receipt bilgileri bulunuyor
                        var receipt = parsedResult["result"];

                        if (receipt != null)
                        {
                            // "logs" kısmı, işlemin log bilgilerini içerir
                            var logs = receipt["logs"];

                            if (logs != null)
                            {
                               //Console.WriteLine($"Logs for Transaction Hash: {transactionHash}");
                                CleanAndPrintLogs(logs);
                            }
                            else
                            {
                                Console.WriteLine("No logs found for Transaction Hash: {transactionHash}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Hata: Receipt bilgisi bulunamadı.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Hata: {response.StatusCode} - {response.ReasonPhrase}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hata: {ex.Message}");
                }
            }
        }
    }

    static void CleanAndPrintLogs(JToken logs)
    {
        foreach (var log in logs)
        {
            var data = log["data"].ToString();

            // Hex stringi ASCII karakterlere çevir, kontrolsüz karakterleri temizle ve ekrana yazdır
            var cleanedData = CleanUnwantedCharacters(HexToAscii(data));
            Console.WriteLine($"İslem Bilgisi: {AddSpaceAfterEachWord(cleanedData)}");
        }
    }

    static string HexToAscii(string hex)
    {
        StringBuilder ascii = new StringBuilder();

        for (int i = 0; i < hex.Length; i += 2)
        {
            string hexByte = hex.Substring(i, 2);
            try
            {
                // Hex'i ASCII karaktere çevir
                byte asciiByte = Convert.ToByte(hexByte, 16);
                char asciiChar = Convert.ToChar(asciiByte);
                ascii.Append(asciiChar);
            }
            catch (Exception)
            {
                // Geçersiz hex karakteri, boşluk ekleyerek devam et
                ascii.Append(" ");
            }
        }

        return ascii.ToString();
    }

    static string CleanUnwantedCharacters(string input)
    {
        // ASCII dışındaki tüm karakterleri temizle
        var cleaned = new string(input.Where(c => c >= 32 && c < 127).ToArray());

        return cleaned.Trim(); // Başındaki ve sonundaki boşlukları kaldır
    }

    static string AddSpaceAfterEachWord(string input)
    {
        StringBuilder result = new StringBuilder();

        foreach (char c in input)
        {
            // Her büyük harfin önüne bir boşluk ekle
            if (char.IsUpper(c) && result.Length > 0)
            {
                result.Append(' ');
            }

            result.Append(c);
        }

        return result.ToString();
    }
}
