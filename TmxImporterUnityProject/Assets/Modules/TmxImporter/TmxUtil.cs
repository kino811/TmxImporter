using UnityEngine;
using System.Collections;
using System.IO;

namespace Kino.Tmx {
    public class TmxUtil {
        public static void CreateDirectory(string dirPath) {
            if (!System.IO.Directory.Exists(dirPath)) {
                System.IO.Directory.CreateDirectory(dirPath);
            }
        }
    }
}
