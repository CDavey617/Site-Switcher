using System.ComponentModel.Composition;
using System.Windows;

namespace CraigDavey.Nina.Sites.Features.Settings {
    [Export(typeof(ResourceDictionary))]
    public partial class Resources : ResourceDictionary {
        public Resources() {
            InitializeComponent();
            var key = new Plugin().Name + "_Options";
            this[key] = this["OptionsTemplate"];
            Remove("OptionsTemplate");
        }
    }
}
