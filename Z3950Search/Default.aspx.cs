using System;
using Zoom.Net.YazSharp;
using System.Xml;

namespace Z3950Search
{
    using System.IO;
    using System.Text;
    using System.Xml.Xsl;

    public partial class Default : System.Web.UI.Page
    {
        public const int MaximumNumberOfResults = 100;
        
        public const string TransformationXslt =
            @"<xsl:stylesheet version=""1.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"" xmlns:ns0=""http://www.altova.com/xslt-extensions"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" exclude-result-prefixes=""ns0 xs"">
	                <xsl:output method=""xml"" indent=""yes""/>
	                <xsl:param name=""input"" select=""/..""/>
	                <xsl:template match=""/"">
		                <xsl:variable name=""var1_initial"" select="".""/>
			                <Items>
                                <DataSource>CC0C9385-CD07-4B90-96B7-E8ADE13F6DC2</DataSource>
				                <Rank>
					                0
				                </Rank>
				                <xsl:for-each select=""record-list"">
					                <xsl:variable name=""var11_current"" select="".""/>
					                <Title>
						                <xsl:value-of select=""dc-record/title""/>
					                </Title>
				                </xsl:for-each>
				                <xsl:for-each select=""record-list/dc-record/description"">
					                <xsl:variable name=""var12_current"" select="".""/>
					                <Summary>
						                <xsl:value-of select="".""/>
					                </Summary>
				                </xsl:for-each>
					                <Date>
						                2003-11-03T00:00:00Z
					                </Date>
				                <Authors>
					                <SearchResultAuthor>
						                <xsl:for-each select=""record-list"">
							                <xsl:variable name=""var13_current"" select="".""/>
							                <Name>
								                <xsl:value-of select=""dc-record/contributor""/>
							                </Name>
						                </xsl:for-each>
					                </SearchResultAuthor>
				                </Authors>
				                <Categories>
					                <xsl:for-each select=""record-list"">
						                <xsl:variable name=""var14_current"" select="".""/>
						                <string>
							                <xsl:value-of select=""dc-record/type""/>
						                </string>
					                </xsl:for-each>
				                </Categories>
                                <Links>
                                    <SearchResultLink>
                                    <Title>Oxford</Title>
                                    <Url>https://www.ox.ac.uk/research/libraries?wssl=1</Url>
                                    </SearchResultLink>
                                </Links>
			                </Items>
	                </xsl:template>
                </xsl:stylesheet>";
        
        protected void Page_Load(object sender, EventArgs e)
        {
            // Create a connection object to the server
            // In this example, I am connecting to Oxford library
            // For a list of publicly known Z39.50 endpoints, refer to:
            // http://www.loc.gov/z3950/gateway.html
            var connection = new Connection("library.ox.ac.uk", 210)
                                 {
                                     DatabaseName = "ADVANCE",
                                     Syntax = Zoom.Net.RecordSyntax.XML
                                 };
            // Connect
            connection.Connect();
            
            // Declare the query either in PQF or CQL according to whether the Z39.50 implementation at the endpoint support it.
            // The following query is in PQF format
            var query = "@attr 1=4 kennedy"; 
            var q = new PrefixQuery(query);

            // Get the search results in binary format
            // Note that each result is in XML format
            var results = connection.Search(q);

            // Limit the number of results either by the maximum number of results allowed or by the number of results returned
            var numberOfResults = results.Count < MaximumNumberOfResults ? results.Count : MaximumNumberOfResults;

            var resultXml = new StringBuilder("<SearchResultCollection>");
            
            for (uint i = 0; i < numberOfResults; i++)
            {
                try
                {
                    resultXml.Append(
                        Transform(
                            Encoding.UTF8.GetString(results[i].Content),
                            TransformationXslt));
                }
                catch
                {
                    //// todo: log the exception
                }
            }

            resultXml.Append("</SearchResultCollection>");

            this.ltrlBookDetails.Text = resultXml.ToString().Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>", string.Empty);
        }

        private static string Transform(string toBeTransFormed, string transformationFile)
        {
            string output = string.Empty;

            if (string.IsNullOrEmpty(toBeTransFormed))
            {
                return output;
            }

            var xmlReaderSettings = new XmlReaderSettings() { DtdProcessing = DtdProcessing.Ignore };

            using (StringReader srt = new StringReader(transformationFile))
            {
                using (StringReader sri = new StringReader(toBeTransFormed))
                {
                    using (XmlReader xrt = XmlReader.Create(srt, xmlReaderSettings))
                    {
                        using (XmlReader xri = XmlReader.Create(sri, xmlReaderSettings))
                        {
                            XslCompiledTransform xslt = new XslCompiledTransform();
                            xslt.Load(xrt);
                            using (StringWriter sw = new StringWriter())
                            {
                                using (XmlWriter xwo = XmlWriter.Create(sw, xslt.OutputSettings))
                                {
                                    xslt.Transform(xri, xwo);
                                    output = sw.ToString();
                                }
                            }
                        }
                    }
                }
            }

            return output;
        }
    }
}