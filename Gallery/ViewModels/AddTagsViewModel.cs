namespace Gallery.ViewModels
{
    using System;
    using System.Reactive;
    using System.Reactive.Linq;

    using Gallery.Models;

    using ReactiveUI;

    public class AddTagsViewModel : DialogViewModelBase
    {
        private string _name = string.Empty;
        private string _value = string.Empty;

        public AddTagsViewModel()
        {
            WindowTitle = "Add tags";

            var canExecute = this.WhenAnyValue(x => x.Name)
                .Select(name => !string.IsNullOrWhiteSpace(name));

            AddTagsCommand = ReactiveCommand.Create(AddTagsAndClose, canExecute);
        }

        public string Name { get => _name; set => this.RaiseAndSetIfChanged(ref _name, value); }

        public string Value { get => _value; set => this.RaiseAndSetIfChanged(ref _value, value); }

        public ReactiveCommand<Unit, Unit> AddTagsCommand { get; }

        public void AddTagsAndClose()
        {
            Tag tag = string.IsNullOrWhiteSpace(Value) ? new Tag(Name) : new Tag(Name, Value);
            CloseCommand.Execute(tag).Subscribe();
        }
    }
}
