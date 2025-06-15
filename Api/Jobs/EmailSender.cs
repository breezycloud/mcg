using System.Reflection;
using FluentEmail.Core;
using Microsoft.EntityFrameworkCore;
using RazorLight;
using Api.Context;
using Shared.Models.Auth;
// using Shared.Models.Notifications;
// using Shared.Models.Users;

// namespace Api.Services;

// public class EmailSender(IFluentEmail _fluentEmail, 
//         AppDbContext _context, 
//         IConfiguration _config,
//         RazorLightEngine _razorEngine, ILogger<EmailSender> _logger) : IDisposable
// {
//     const int _ExpirationMinutes = 15;
//     const string otpTemplate = "OtpEmailTemplate.cshtml";
//     public async Task SendVerificationEmail(Driver driver)
//     {
//         await Task.Delay(0);
//     }
//     private bool IsRunning = false;
//     private string? Base64 = "";
//     public async Task SendOtpCodes()
//     {
//         if (IsRunning)
//         {
//             _logger.LogInformation("OTP mail is already in progress");
//             return;
//         }      
//         using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Api.Assets.icon-192.png");
//         using (var reader = new BinaryReader(stream!))
//         {
//             var imageBytes = reader.ReadBytes((int)stream!.Length);
//             Base64 = Convert.ToBase64String(imageBytes);
//         }   
//         IsRunning = true;
//         _logger.LogInformation("OTP mail is in progress");
//         var otps = _context.OtpCodes.AsNoTracking().AsSplitQuery().Include(x => x.User).AsParallel().Where(x => !x.Sent).ToArray();
//         foreach (var otp in otps)
//         {
//             await SendOtp(otp);            
//         }
//         IsRunning = false;        
//     }

//     public async Task SendOtp(OtpCode otp)
//     {
//         try
//         {
//             var model = new
//             {
//                 Logo = Base64,
//                 Name = otp!.User!.ToString(),
//                 OtpCode = otp!.Otp,
//                 ExpirationMinutes = _ExpirationMinutes
//             };        
//             string mailBody = await _razorEngine.CompileRenderAsync(otpTemplate, model);        
//             var email = await _fluentEmail.To(otp.User!.Email)
//                                         .Subject("Your OTP Code")
//                                         .Body(mailBody, true)
//                                         .SendAsync();
//             if (email.Successful)            
//             {
//                 await _context.OtpCodes.Where(x => x.Id == otp.Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.Sent, true));
//             }
//         }
//         catch (System.Exception ex)
//         {
//             _logger.LogError(ex, "Failed to send OTP email");
//         }        
//     }    
    
//     public async Task NotifyAdminNewDriver(NewDriverMail model)
//     {
//         var mail = _config["MailOptions:Email"];
//         if (string.IsNullOrEmpty(mail))
//         {
//             _logger.LogError("Admin email is not configured.");
//             return;
//         }
//         try
//         {
//             string mailBody = await _razorEngine.CompileRenderAsync("RegistrationNotification.cshtml", model);        
//             var email = await _fluentEmail.To(mail)
//                                         .Subject("New Driver Registration")
//                                         .Body(mailBody, true)
//                                         .SendAsync();
//             if (email.Successful)            
//             {
//                 _logger.LogInformation("New driver registration notification sent to admin");
//             }
//         }
//         catch (System.Exception)
//         {
//             _logger.LogError("Failed to send new driver registration notification to admin");            
//             throw;
//         }        
//     }

//     public async Task DriverConfirmation(DriverConfirmationMail model)
//     {
//         var mail = _config["MailOptions:Email"];
//         if (string.IsNullOrEmpty(mail))
//         {
//             _logger.LogError("Admin email is not configured.");
//             return;
//         }
//         try
//         {
//             string mailBody = await _razorEngine.CompileRenderAsync("ConfirmationTemplate.cshtml", model);        
//             var email = await _fluentEmail.To(model.Email)
//                                         .Subject($"Confirmation")
//                                         .Body(mailBody, true)
//                                         .SendAsync();
//             if (email.Successful)            
//             {
//                 _logger.LogInformation("Confirmation notification sent to driver");
//             }
//         }
//         catch (System.Exception)
//         {
//             _logger.LogError("Confirmation notification to driver");
//             throw;
//         }        
//     }

//     public async Task SendOtp(OtpCode[] otps)
//     {
//         foreach (var otp in otps)
//         {
//             var email = await _fluentEmail
//             .To(otp.User!.Email)
//             .Subject("Your OTP Code")
//             .UsingTemplateFromFile($"{Directory.GetCurrentDirectory()}/EmailTemplates/OtpEmailTemplate.cshtml", new
//             {
//                 Name = otp!.User!.ToString(),
//                 OtpCode = otp!.Otp,
//                 ExpirationMinutes = _ExpirationMinutes
//             })
//             .SendAsync();

//             if (!email.Successful)
//             {            
//                 continue;
//             }
//         }        
//     }    

//     public void Dispose()
//     {
        
//     }
// }