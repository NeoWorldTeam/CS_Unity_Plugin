namespace ComeSocial.Face.Drive
{
    /// <inheritdoc />
    /// <summary>
    /// 访问steamreader的接口
    /// </summary>
    public interface IStreamSource : IUsesStreamReader
    {
        /// <summary>
        /// StreamSource的IStreamSettings数据
        /// </summary>
        IStreamSettings streamSettings { get; }

        /// <summary>
        /// 是否正在更新跟踪数据
        /// </summary>
        bool active { get; }

        /// <summary>
        /// 在StreamReader 更新后，更新源
        /// </summary>
        void StreamSourceUpdate();
    }
}
