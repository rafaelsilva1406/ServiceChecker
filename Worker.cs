namespace SmartSyncWorkerService
{
    using System.Diagnostics;
    using System.Net;
    using System.Net.Mail;
    using System.ServiceProcess;
    using Microsoft.Extensions.Options;

    public class Service
    {
        public string ServiceName { get; set; }
        public string ProcessName { get; set; }
        public int Maxmemory {get; set; }
    }
    
    public class AppConfiguration
    {
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public Service[] Services { get; set; }
    }

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private AppConfiguration _appConfiguration;

        private ServiceController[] _services = ServiceController.GetServices();


        private string[] _stoppedService;

        public Worker(ILogger<Worker> logger, IOptions<AppConfiguration> appConfiguration)
        {
            _logger = logger;
            _appConfiguration = appConfiguration.Value;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            // DO YOUR STUFF HERE
            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            // DO YOUR STUFF HERE
            await base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (stoppingToken.IsCancellationRequested is false)
                {
                    // assign counter
                    int i = -1;
                    // instance Email class
                    SmtpClient _smtpClient = new SmtpClient(_appConfiguration.SmtpHost, _appConfiguration.SmtpPort);
                    // pass creds
                    NetworkCredential _networkCredential = new NetworkCredential(_appConfiguration.SmtpUsername, _appConfiguration.SmtpPassword);
                    // bind creds to email class
                    _smtpClient.Credentials = _networkCredential;
                    // enable ssl
                    _smtpClient.EnableSsl = true;
                    foreach (var s in _appConfiguration.Services)
                    {
                        //add counter
                        i++;

                        //instance service controller class by service name
                        ServiceController sc = new ServiceController(s.ServiceName.Trim());

                        //refresh latest status
                        sc.Refresh();

                        //check status not stopped or stopPending
                        if (sc.Status.ToString().Trim() == "Stopped" || sc.Status.ToString().Trim() == "StopPending")
                        {
                            // setup email
                            MailMessage _mailMessage = new MailMessage
                            {
                                From = new MailAddress(_appConfiguration.From),
                                Subject = _appConfiguration.Subject,
                                Body = sc.DisplayName + _appConfiguration.Body,
                                IsBodyHtml = true
                            };

                            _mailMessage.To.Add(_appConfiguration.To);
                            //try send email
                            try
                            {
                                _logger.LogWarning("Service: {service} Status: {status}", sc.DisplayName, sc.Status);
                                _smtpClient.Send(_mailMessage);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError("Error Mail {s}: {m}", sc.DisplayName, ex.Message);
                            }
                        } else {
                            //instance server performance
                            PerformanceCounter PC = new PerformanceCounter();
                            //check by process
                            PC.CategoryName = "Process";
                            //name counter
                            PC.CounterName = "Working Set - Private";
                            //set process to lookup
                            PC.InstanceName = s.ProcessName.Trim();
                            //obtain counter value and convert into mb
                            int memsize = Convert.ToInt32(PC.NextValue()) / (int)(1024 * 1024);
                            //Gb perfomance counter
                            PC.Close();
                            PC.Dispose();

                            if(memsize > s.Maxmemory ){
                                string emailbody = $"Service {sc.DisplayName} with Process {s.ProcessName} exceeded size limit: {memsize}";
                                 // setup email
                                MailMessage _mailMessage = new MailMessage
                                {
                                    From = new MailAddress(_appConfiguration.From),
                                    Subject = _appConfiguration.Subject,
                                    Body = emailbody,
                                    IsBodyHtml = true
                                };

                                _mailMessage.To.Add(_appConfiguration.To);
                                //try send email
                                try
                                {
                                    //Add limit to not exceed 1587
                                    _logger.LogWarning("Service {name} with Process {process} exceeded size limit: {memory}", sc.DisplayName, s.ProcessName, memsize);
                                    _smtpClient.Send(_mailMessage);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError("Error Mail {s}: {m}", sc.DisplayName, ex.Message);
                                }
                            }
                        }
                    }
                    //delay 5min
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError("Error: {m}", ex.Message);

                Environment.Exit(1);
            }
        }

        public override void Dispose()
        {
            // DO YOUR STUFF HERE
        }
    }
}