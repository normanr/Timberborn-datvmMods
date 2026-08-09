namespace ModdableTimberborn.DependencyInjection;

public class EditableBlueprint(string name)
{

    public string Name { get; set; } = name;
    public List<EditableBlueprint> Children { get; set; } = [];
    public List<ComponentSpec> Specs { get; set; } = [];
    BlueprintFileBundle? source;

    public EditableBlueprint(Blueprint blueprint) : this(blueprint.Name)
    {
        Children = [.. blueprint.Children.Select(static q => new EditableBlueprint(q))];
        Specs = [.. blueprint.Specs];
        try {
            source = ContainerRetriever.GetInstance<BlueprintSourceService>().Get(blueprint);
        }
        catch
        {
        }
    }

    public EditableBlueprint(string name, ComponentSpec spec) : this(name)
    {
        Specs.Add(spec);
        try {
            source = ContainerRetriever.GetInstance<BlueprintSourceService>().Get(spec.Blueprint);
        }
        catch
        {
        }
    }

    public void TransformSpecs(Func<ComponentSpec, ComponentSpec?> transformer)
    {
        for (int i = 0; i < Specs.Count; i++)
        {
            var modified = transformer(Specs[i]);
            if (modified is not null)
            {
                Specs[i] = modified;
                if (source != null)
                {
                    source = source.AddJson("{\"" + Specs[i].GetType().Name + "\": {}}", transformer.Method.DeclaringType.Name + "." + transformer.Method.Name);
                }
            }
        }
    }

    public void TransformSpec<T>(Func<T, T?> transform) where T : ComponentSpec
        => TransformSpecs(s => (s is T t) ? transform(t) : null);

    public T GetSpec<T>() where T : ComponentSpec => Specs.OfType<T>().First();

    public Blueprint ToBlueprint()
    {
        Blueprint blueprint = new(Name, Specs, [.. Children.Select(static q => q.ToBlueprint())]);
        if (source != null)
        {
            ContainerRetriever.GetInstance<BlueprintSourceService>().Add(blueprint, source);
        }
        return blueprint;
    }
    public static implicit operator Blueprint(EditableBlueprint editableBlueprint) => editableBlueprint.ToBlueprint();

}
