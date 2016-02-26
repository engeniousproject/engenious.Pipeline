using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace engenious.Content.Serialization
{
    public class SerializationManager
    {
        private static SerializationManager instance;

        public static SerializationManager Instance
        { 
            get
            {
                if (instance == null)
                    instance = new SerializationManager();
                return instance;
            } 
        }


        //private Dictionary<string ,IContentTypeReader> typeReaders;
        private Dictionary<string ,IContentTypeWriter> typeWriters;

        protected SerializationManager()
        {
            //typeReaders = new Dictionary<string, IContentTypeReader> ();
            typeWriters = new Dictionary<string, IContentTypeWriter>();
            AddAssembly(Assembly.GetExecutingAssembly());
        }

        public void AddAssembly(Assembly assembly)
        {
            foreach (Type t in assembly.GetTypes())
            {
                /*if (t.GetInterfaces ().Contains (typeof(IContentTypeReader)) && t.GetCustomAttribute<ContentTypeReaderAttribute> () != null) {
					IContentTypeReader reader = Activator.CreateInstance (t) as IContentTypeReader;
					typeReaders.Add (t.Namespace + "." + t.Name, reader);
				} else*/
                if (t.GetInterfaces().Contains(typeof(IContentTypeWriter)) && t.GetCustomAttributes(typeof(ContentTypeWriterAttribute), true).FirstOrDefault() != null)
                {
                    IContentTypeWriter writer = Activator.CreateInstance(t) as IContentTypeWriter;

                    typeWriters.Add(writer.RuntimeType.Namespace + "." + writer.RuntimeType.Name, writer);
                }
            }
        }


        /*public IContentTypeReader GetReader (string reader)
		{
			IContentTypeReader res;
			if (!typeReaders.TryGetValue (reader, out res))
				return null;
			return res;
		}*/

        public IContentTypeWriter GetWriter(Type writerType)
        {
            IContentTypeWriter res;
            if (!typeWriters.TryGetValue(writerType.FullName, out res))
                return null;
            return res;
		
        }
    }
}