using System;
using System.IO;
using Xunit;

namespace Kiota.Builder.Writers.Ruby.Tests {
    public class CodeIndexerWriterTests : IDisposable
    {
        private const string defaultPath = "./";
        private const string defaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeClass parentClass;
        private readonly CodeIndexer indexer;
        public CodeIndexerWriterTests() {
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.Ruby, defaultPath, defaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
            parentClass = new CodeClass(root) {
                Name = "parentClass"
            };
            root.AddClass(parentClass);
            indexer = new CodeIndexer(parentClass) {
                Name = "idx",
            };
            indexer.IndexType = new CodeType(indexer) {
                Name = "string",
            };
            indexer.ReturnType = new CodeType(indexer) {
                Name = "SomeRequestBuilder"
            };
            parentClass.SetIndexer(indexer);
        }
        public void Dispose() {
            tw?.Dispose();
        }
        [Fact]
        public void WritesIndexer() {
            writer.Write(indexer);
            var result = tw.ToString();
            Assert.Contains("http_core", result);
            Assert.Contains("path_segment", result);
            Assert.Contains("+ position", result);
            Assert.Contains("def [](position)", result);
        }
    }
}