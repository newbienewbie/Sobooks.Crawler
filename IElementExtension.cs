
using System;
using System.Linq;
using AngleSharp.Dom;


namespace Itminus.Hunters{

    public static class INodeExtensions {

        public static string GetFirstTextContext(this IElement element){
            return element
                ?.ChildNodes
                ?.Where(n => n.NodeType == NodeType.Text)
                ?.FirstOrDefault()
                ?.TextContent;
        }

        // do for-each until end condition : [start,end)
        public static void ForEachUntil(this INode node, Func<INode,bool> endCondition, Action<INode> action){
            if(node == null){ throw new ArgumentNullException(nameof(node));}
            if(action == null){ throw new ArgumentNullException(nameof(action));}
            if(endCondition== null){ throw new ArgumentNullException(nameof(endCondition));}

            var _next =node.NextSibling;
            while(_next!= null){
                if(endCondition(_next)){ break; }
                action(_next);
                _next = _next.NextSibling;
            }
        }

    }


}