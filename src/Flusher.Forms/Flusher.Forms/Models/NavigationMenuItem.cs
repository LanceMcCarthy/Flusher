using System;

namespace Flusher.Forms.Models
{
    public class NavigationMenuItem
    {
        public NavigationMenuItem()
        {
            TargetType = typeof(NavigationMenuItem);
        }

        public int Id { get; set; }

        public string Title { get; set; }

        public Type TargetType { get; set; }
    }
}