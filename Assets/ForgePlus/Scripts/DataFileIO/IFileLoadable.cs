using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForgePlus.DataFileIO
{
    public interface IFileLoadable
    {
        void Load(string fileName);
    }
}
