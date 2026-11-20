using System.ComponentModel.Composition;
using System.Windows;

namespace CraigDavey.Nina.Sites.Features.Dock {
    [Export(typeof(ResourceDictionary))]
    public partial class Resources : ResourceDictionary {
        public Resources() {
            InitializeComponent();
        }
    }
}
