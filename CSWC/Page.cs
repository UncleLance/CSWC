using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CSWC
{
    class Page
    {
        public string Url { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Content {  get; set; }
        public List<Image> Images { get; set; }
    }
}
