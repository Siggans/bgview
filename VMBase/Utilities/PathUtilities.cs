using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace VMBase.Utilities
{
	public class PathUtilities
	{
		public static bool VerifyDirectoryWritePermission(string path)
		{
			if (Directory.Exists(path))
			{
				var acl = Directory.GetAccessControl(path);
				if (acl == null) return false;
				var rules = acl.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
				if (rules == null) return false;

				var writeAllow = false;
				var writeDeny = false;
				foreach (var obj in rules)
				{
					if (obj is FileSystemAccessRule)
					{
						var rule = obj as FileSystemAccessRule;
						if ((FileSystemRights.Write & rule.FileSystemRights) == FileSystemRights.Write)
						{
							if (rule.AccessControlType == AccessControlType.Allow) writeAllow = true;
							else if (rule.AccessControlType == AccessControlType.Deny) writeDeny = true;
						}
					}
				}
				return writeAllow && !writeDeny;
			}
			return false;

		}

	}
}
