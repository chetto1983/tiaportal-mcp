using Siemens.Engineering;
using Siemens.Engineering.SW.Blocks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TiaMcpServer.ModelContextProtocol
{
    public class Helper
    {
        public static List<Attribute> GetAttributeList(IEngineeringObject obj)
        {
            var attributes = new List<Attribute>();

            if (obj != null)
            {
                foreach (var attr in obj.GetAttributeInfos())
                {
                    object? value;
                    try
                    {
                        value = ToSerializableValue(obj.GetAttribute(attr.Name));
                    }
                    catch (Exception ex)
                    {
                        // One unreadable attribute must not fail the whole tool call
                        value = $"<error: {ex.Message}>";
                    }

                    attributes.Add(new Attribute
                    {
                        Name = attr.Name,
                        Value = value,
                        AccessMode = Enum.GetName(typeof(EngineeringAttributeAccessMode), attr.AccessMode)
                    });
                }
            }

            return attributes;
        }

        /// <summary>
        /// Attribute values of devices and device items include Openness objects (Container,
        /// CommentML, certificates, Items). Passed through as-is they make the JSON serialization of
        /// the response fail, so only plain values are kept and everything else is reduced to text.
        /// </summary>
        private static object? ToSerializableValue(object? value)
        {
            switch (value)
            {
                case null:
                    return null;
                case string _:
                case bool _:
                case Enum _:
                case DateTime _:
                case TimeSpan _:
                case Guid _:
                case decimal _:
                    return value;
                case MultilingualText text:
                    return FirstText(text);
                case IEngineeringObject engineeringObject:
                    return GetNameOrTypeName(engineeringObject);
                case IEnumerable items:
                    return items.Cast<object?>().Select(ToSerializableValue).ToList();
            }

            return value.GetType().IsPrimitive ? value : value.ToString();
        }

        private static string GetNameOrTypeName(IEngineeringObject engineeringObject)
        {
            try
            {
                if (engineeringObject.GetAttribute("Name") is string name)
                {
                    return name;
                }
            }
            catch (Exception)
            {
                // the object has no Name attribute
            }

            return engineeringObject.GetType().Name;
        }

        /// <summary>
        /// First available translation of a MultilingualText (tag and constant comments), or
        /// null when the text has no items. Openness returns one item per project language.
        /// </summary>
        public static string? FirstText(MultilingualText? text)
        {
            if (text == null)
            {
                return null;
            }

            foreach (var item in text.Items)
            {
                return item.Text;
            }

            return null;
        }

        public static BlockGroupInfo BuildBlockHierarchy(PlcBlockGroup group, TiaMcpServer.Siemens.Portal? portal = null)
        {
            var groupInfo = new BlockGroupInfo
            {
                Name = group.Name
            };

            var blockList = new List<ResponseBlockInfo>();
            foreach (var block in group.Blocks)
            {
                var attributes = Helper.GetAttributeList(block);
                blockList.Add(new ResponseBlockInfo
                {
                    Path = portal == null ? null : portal.GetBlockPath(block),
                    Name = block.Name,
                    TypeName = block.GetType().Name,
                    Namespace = block.Namespace,
                    ProgrammingLanguage = Enum.GetName(typeof(ProgrammingLanguage), block.ProgrammingLanguage),
                    MemoryLayout = Enum.GetName(typeof(MemoryLayout), block.MemoryLayout),
                    IsConsistent = block.IsConsistent,
                    HeaderName = block.HeaderName,
                    ModifiedDate = block.ModifiedDate,
                    IsKnowHowProtected = block.IsKnowHowProtected,
                    Attributes = attributes,
                    Description = block.ToString()
                });
            }
            groupInfo.Blocks = blockList;

            var groupList = new List<BlockGroupInfo>();
            foreach (var subGroup in group.Groups)
            {
                groupList.Add(BuildBlockHierarchy(subGroup, portal));
            }
            groupInfo.Groups = groupList;

            return groupInfo;
        }
    }
}
