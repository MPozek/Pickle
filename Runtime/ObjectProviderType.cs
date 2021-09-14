namespace Pickle
{
    public enum ObjectProviderType
    {
        None = 0,
        Assets = 1 << 0,
        Scene = 1 << 1,
        Children = 1 << 2,
        RootChildren = 1 << 3,

        Default = 1 << 31,
    }
}