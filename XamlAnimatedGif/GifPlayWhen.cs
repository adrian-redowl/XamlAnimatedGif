namespace XamlAnimatedGif
{
    /// <summary>
    /// What starts and stops a <see cref="GifImage"/>.
    /// Stop returns to the first frame and holds it.
    /// </summary>
    public enum GifPlayWhen
    {
        /// <summary>Play as soon as the image is loaded.</summary>
        Always,

        /// <summary>Play while <see cref="GifImage.IsSelected"/> is true.</summary>
        Selected,

        /// <summary>Play while the pointer is over the control.</summary>
        MouseOver
    }
}
