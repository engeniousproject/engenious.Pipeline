using System;
using System.Collections.Generic;

namespace engenious.Content.Pipeline
{
	public class ContentContext
	{
		public ContentContext ()
		{
			Dependencies = new List<string> ();

		}

		public List<string> Dependencies{ get; private set; }

		public void AddDependency (string file)
		{
			Dependencies.Add (file);
		}
	}
}

