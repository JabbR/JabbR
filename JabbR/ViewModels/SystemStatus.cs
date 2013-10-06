using System;

namespace JabbR.ViewModels
{
    public class SystemStatus
    {
        public string SystemName { get; set; }
        
        public string StatusMessage { get; set; }

        public bool? IsOK { get; set; }

        public string StatusCssClass
        {
            get
            { 
                return IsOK.HasValue
                    ? IsOK.Value
                        ? "text-success"
                        : "text-error"
                    : "text-info";
            }
        }

        public void SetOK(string message = "OK")
        {
            IsOK = true;
            StatusMessage = message;
        }

        public void SetException(Exception ex)
        {
            IsOK = false;
            StatusMessage = String.Format("Error: " + ex.ToString());
        }
    }
}