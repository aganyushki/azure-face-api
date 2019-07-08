using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceAPI
{
    class IdentifyResponce
    {
        public string faceId { get; set; }
        public Candidates[] candidates { get; set; }
    }
}
