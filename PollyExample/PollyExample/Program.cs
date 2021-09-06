using Polly;
using Polly.CircuitBreaker;
using System;
using System.IO;
using System.Net;
using System.Threading;

namespace PollyExample
{
    class Program
    {
        static void Main(string[] args)
        {
            //Política de Waint and Retry - Espera 2 segundo para tentar novamente
            var retry = Policy.Handle<WebException>()
                              .WaitAndRetryForever(attemp => {
                                  Console.WriteLine($"Nova Tentativa");
                                  return TimeSpan.FromSeconds(2);
                              }
                              );
            //Política de Circuit Breaker - Deixa o Circuito fechado por 5 segundos impedindo novos requests
            var circuitBreakerPolicy = Policy.Handle<WebException>().CircuitBreaker(3, TimeSpan.FromSeconds(5), onBreak: (ex, timespan, context) =>
            {
                Console.WriteLine("Circuito entrou em estado de falha");
            }, onReset: (context) =>
            {
                Console.WriteLine("Circuito saiu do estado de falha");
            });


            while (true)
            {
                retry.Execute(() =>
                {
                    if (circuitBreakerPolicy.CircuitState != CircuitState.Open)
                    {
                        circuitBreakerPolicy.Execute(() =>
                        {
                            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://localhost");
                            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                            using (Stream stream = response.GetResponseStream())
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                var html = reader.ReadToEnd();
                                Console.WriteLine("Requisição feita com sucesso");
                                Thread.Sleep(300);
                            }
                        });
                    }
                });
            }
        }
    }
}