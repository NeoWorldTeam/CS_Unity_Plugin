using System;

namespace ComeSocial.Face.Drive
{
    /// <summary>
    /// Interface for getting access to the stream reader
    /// </summary>
    public interface IUsesStreamReader
    {
        /// <summary>
        /// The StreamReader that controls the session
        /// </summary>
        IStreamReader streamReader { set; }
    }
}
