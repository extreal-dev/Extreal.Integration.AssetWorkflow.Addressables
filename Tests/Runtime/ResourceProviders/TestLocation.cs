using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Extreal.Integration.Assets.Addressables.ResourceProviders.Test
{
    public class TestLocation : IResourceLocation
    {
        public override string ToString() => nameof(TestLocation);

        public string InternalId => throw new NotImplementedException();

        public string ProviderId => throw new NotImplementedException();

        public IList<IResourceLocation> Dependencies => throw new NotImplementedException();

        public int DependencyHashCode => throw new NotImplementedException();

        public bool HasDependencies => throw new NotImplementedException();

        public object Data => throw new NotImplementedException();

        public string PrimaryKey => throw new NotImplementedException();

        public Type ResourceType => throw new NotImplementedException();

        public int Hash(Type resultType) => throw new NotImplementedException();
    }
}
