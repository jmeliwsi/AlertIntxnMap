using System;
using System.Collections.Generic;
using System.Text;

namespace WSIMap
{
    internal static class MapGLPool
    {
        private const int poolSize = 0;
        private static LinkedList<MapGL> pool = null;

        static MapGLPool()
        {
            // NOTE: This is commented out for now so it doesn't use any memory

            //pool = new LinkedList<MapGL>();

            //for (int i = 0; i < poolSize; i++)
            //{
            //    MapGL mapGL = new MapGL();
            //    GC.SuppressFinalize(mapGL);
            //    mapGL.InitializeContexts();
            //    mapGL.pooled = true;
            //    pool.AddLast(mapGL);
            //}
        }

        public static MapGL Allocate()
        {
            if (pool.Count > 0)
            {
                MapGL mapGL = pool.First.Value;
                pool.RemoveFirst();
                return mapGL;
            }
            else
            {
                throw new WSIMapException("The MapGLPool is empty.");
            }
        }

        public static void Release(MapGL mapGL)
        {
            if (pool.Count < poolSize)
                pool.AddFirst(mapGL);
        }

        public static void Dispose()
        {
            while (pool.Count > 0)
            {
                MapGL mapGL = pool.First.Value;
                pool.RemoveFirst();
                mapGL.pooled = false;
                mapGL.Dispose();
            }
        }
    }
}
