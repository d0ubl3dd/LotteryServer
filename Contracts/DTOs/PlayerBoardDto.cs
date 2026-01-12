using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts.DTOs
{
    public class PlayerBoardDto
    {
        public int PlayerId { get; set; }
        public int BoardId { get; set; }
        public List<int> MarkedPositions { get; set; }
    }

}
