using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace engenious.Content.Serialization
{
    /// <summary>
    ///     Manager class for engenious content serialization.
    /// </summary>
    public class SerializationManager
    {
        private static SerializationManager? _instance;

        /// <summary>
        ///     Gets the singleton instance of the <see cref="SerializationManager"/>.
        /// </summary>
        public static SerializationManager Instance => _instance ??= new SerializationManager();


        //private Dictionary<string ,IContentTypeReader> typeReaders;
        private readonly Dictionary<string ,IContentTypeWriter> _typeWriters;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SerializationManager"/> class.
        /// </summary>
        protected SerializationManager()
        {
            //typeReaders = new Dictionary<string, IContentTypeReader> ();
            _typeWriters = new Dictionary<string, IContentTypeWriter>();
            AddAssembly(Assembly.GetExecutingAssembly());
        }

        /// <summary>
        ///     Adds an assemblies <see cref="IContentTypeWriter"/> implementations to the available serialization options.
        /// </summary>
        /// <param name="assembly">The assembly to search through.</param>
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
                    if (Activator.CreateInstance(t) is IContentTypeWriter writer)
                        _typeWriters.Add(writer.RuntimeType.Namespace + "." + writer.RuntimeType.Name, writer);
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

        /// <summary>
        ///     Gets a matching <see cref="IContentTypeWriter"/> that can serialize a given type.
        /// </summary>
        /// <param name="writerType">The type to search a suitable <see cref="IContentTypeWriter"/> for.</param>
        /// <returns>The matching <see cref="IContentTypeWriter"/>.</returns>
        public IContentTypeWriter? GetWriter(Type writerType)
        {
            return writerType.FullName != null && _typeWriters.TryGetValue(writerType.FullName, out var res) ? res : null;
        }
    }
}