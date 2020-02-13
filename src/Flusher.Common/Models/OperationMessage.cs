using System;
using CommonHelpers.Common;

namespace Flusher.Common.Models
{
    public class OperationMessage : BindableBase
    {
        private DateTime timeStamp;
        private string message;
        private string imageUrl;
        private string requester;

        public DateTime TimeStamp
        {
            get => timeStamp;
            set => SetProperty(ref timeStamp, value);
        }

        public string Message
        {
            get => message;
            set => SetProperty(ref message, value);
        }

        public string ImageUrl
        {
            get => imageUrl;
            set => SetProperty(ref imageUrl, value);
        }

        public string Requester
        {
            get => requester;
            set => SetProperty(ref requester, value);
        }
    }
}
