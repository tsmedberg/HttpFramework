using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpFramework
{
    public delegate void HttpRequstHandler(ref HttpRequest req, ref HttpResponse res);
}
