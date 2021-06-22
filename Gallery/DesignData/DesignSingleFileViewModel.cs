namespace Gallery.DesignData
{
    using System.Collections.Generic;
    using System.Drawing;

    using Gallery.Models;

    public class DesignSingleFileViewModel
    {
        static DesignSingleFileViewModel()
        {
            Tags = new HashSet<Tag>
            {
                new Tag("Tag", "Value"),
                new Tag("Hello"),
                new Tag("Tag", "Another value"),
                new Tag("here's another long tag i dunno i'm not feeling creative right now"),
            };
        }

        public static string? UrlPathSegment => "file_name.png";

        public static string Description => @"Narrator: According to all known laws of aviation, there is no way that a bee should be able to fly. Its wings are too small to get its fat little body off the ground. The bee, of course, flies anyway because bees don't care what humans think is impossible.

        Barry: Yellow, black. Yellow, black. Yellow, black. Yellow, black. Ooh, black and yellow! Yeah, let's shake it up a little.

        Mom (Janet Benson) (calling from downstairs:) Barry! Breakfast is ready!";

        public static ISet<Tag>? Tags { get; }

        public static Bitmap? Image => null;
    }
}
