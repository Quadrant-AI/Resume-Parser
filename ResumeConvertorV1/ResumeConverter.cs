using DocumentFormat.OpenXml.Packaging;
using HtmlToOpenXml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Microsoft.Extensions.Configuration;

namespace ResumeConvertorV1
{
    /// <summary>
    /// Main class for converting resumes into structured JSON using OpenAI, rendering to HTML via Razor,
    /// and exporting as a formatted Word document (.docx) using OpenXml and HtmlToOpenXml.
    /// </summary>
    public class ResumeConverter
    {
        private readonly string OpenAiKey = string.Empty; // OpenAI API Key
        private readonly string Model = string.Empty; // OpenAI model used for parsing
        private readonly string TemplateFile = string.Empty; // Path to HTML Razor template
        private readonly string PromptFile = string.Empty; // Path to prompt for GPT
        private readonly string OutputDirectory = string.Empty; // Locaion of target files
        private readonly bool Debug; //Flag to deicde whether or not we will print messages at every step
        private readonly string LogoFile;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public ResumeConverter(bool debug = false)
        {
            // Load config from appsettings.json in current directory
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            // Fetch OpenAI API key from config
            OpenAiKey = config["OpenAI:ApiKey"] ?? throw new Exception("OpenAI API key missing");
            Model = config["OpenAI:Model"] ?? "gpt-4o";
            TemplateFile = config["ResumeConvertor:TemplateFile"];
            PromptFile = config["ResumeConvertor:PromptFile"];
            OutputDirectory = config["ResumeConvertor:OutputDirectory"] ?? "Output";
            LogoFile = config["ResumeConvertor:LogoFile"];
            Debug = debug;

            if (string.IsNullOrEmpty(OpenAiKey))
                throw new InvalidOperationException("OpenAI API key not found in configuration.");
        }

        /// <summary>
        /// Main pipeline method: Reads a resume, extracts text, parses to JSON using GPT,
        /// maps it into the required structure, renders to HTML, and generates .docx.
        /// </summary>
        /// <param name="resumeFilePath">Full path to the resume file (.txt or .pdf)</param>

        public async Task RunAsync(string resumeFilePath)
        {
            // Setup RazorEngine for raw HTML support inside template
            var config = new TemplateServiceConfiguration
            {
                BaseTemplateType = typeof(HtmlTemplateBase<>)
            };
            Engine.Razor = RazorEngineService.Create(config);

            // Step 1: Read text from PDF or TXT file
            string resumeText = ReadResume(resumeFilePath);

            // Step 2: Use OpenAI to parse resume text into structured JSON
            string gptOutput = await ParseResumeWithGptAsync(resumeText);

            // Step 3: Clean model output (strip code block markdown, etc.)
            string cleanedJson = CleanGptOutput(gptOutput);

            // Step 4: Parse cleaned JSON into a JObject (dynamic key access)
            JObject parsedJson = JObject.Parse(cleanedJson);

            // Step 5: Map incoming JSON keys to strongly-typed, structured object for templating
            var mappedJson = TransformJsonKeys(parsedJson);

            if (Debug)
            {
                // Debug: Print mapped model
                Console.WriteLine("==== MAPPED MODEL ====");
                Console.WriteLine(JsonConvert.SerializeObject(mappedJson, Formatting.Indented));
            }

            // Optional: Print education for diagnostics
            dynamic mappedJsonObj = TransformJsonKeys(parsedJson);
            if (Debug)
                Console.WriteLine("EDUCATION TYPE: " + mappedJsonObj.Education?.GetType());
            if (mappedJsonObj.Education != null)
            {
                foreach (var edu in mappedJsonObj.Education)
                    if (Debug)
                        Console.WriteLine(JsonConvert.SerializeObject(edu));
            }

            // Step 6: Render HTML using RazorEngine and template
            string html = RenderHtml(mappedJson);

            // Step 7: Prepare dynamic output paths
            string inputFileName = Path.GetFileNameWithoutExtension(resumeFilePath);
            string outputDir = Path.IsPathRooted(OutputDirectory)
            ? OutputDirectory
            : Path.Combine(Directory.GetCurrentDirectory(), OutputDirectory);
            Directory.CreateDirectory(outputDir); // Ensure Output/ folder exists

            string htmlPath = Path.Combine(outputDir, $"{inputFileName}.html");
            string docxPath = Path.Combine(outputDir, $"{inputFileName}.docx");

            // Step 8: Save HTML and DOCX
            File.WriteAllText(htmlPath, html, System.Text.Encoding.UTF8);
            GenerateDocxFromHtml(html, docxPath);

            Console.WriteLine("HTML and DOCX files generated:");
            Console.WriteLine($"   • {htmlPath}");
            Console.WriteLine($"   • {docxPath}");
        }

        /// <summary>
        /// Reads a resume file and extracts plain text, supporting .txt and .pdf files.
        /// </summary>
        /// <param name="filePath">Path to the resume file</param>
        /// <returns>Extracted text</returns>
        private string ReadResume(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            return ext switch
            {
                ".txt" => File.ReadAllText(filePath),
                ".pdf" => ExtractTextFromPdf(filePath),
                _ => throw new InvalidOperationException("Unsupported format"),
            };
        }

        /// <summary>
        /// Extracts all text from a PDF file using PdfPig.
        /// </summary>
        /// <param name="filePath">Path to the PDF</param>
        /// <returns>Combined text from all PDF pages</returns>
        private string ExtractTextFromPdf(string filePath)
        {
            var builder = new StringBuilder();
            using var doc = PdfDocument.Open(filePath);
            foreach (Page page in doc.GetPages())
            {
                builder.AppendLine(page.Text);
            }
            return builder.ToString().Trim();
        }

        /// <summary>
        /// Sends resume text and a system prompt to the OpenAI API to extract structured resume data in JSON format.
        /// </summary>
        /// <param name="resumeText">Plain text of the resume</param>
        /// <returns>Raw GPT output (should be a JSON string, but may include code fences)</returns>
        private async Task<string> ParseResumeWithGptAsync(string resumeText)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {OpenAiKey}");

            var payload = new
            {
                model = Model,
                messages = new object[]
                {
                    new { role = "system", content = File.ReadAllText(PromptFile) },
                    new { role = "user", content = resumeText }
                },
                temperature = 0.0
            };

            var response = await client.PostAsync(
                "https://api.openai.com/v1/chat/completions",
                new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json")
            );

            string result = await response.Content.ReadAsStringAsync();
            JObject root = JObject.Parse(result);
            // Extract the JSON part of GPT response
            return root["choices"]?[0]?["message"]?["content"]?.ToString() ?? throw new Exception("No GPT content");
        }

        /// <summary>
        /// Cleans the GPT model's output by stripping triple-backtick code blocks and whitespace.
        /// </summary>
        /// <param name="gptOutput">Raw GPT response</param>
        /// <returns>Cleaned JSON string</returns>
        private string CleanGptOutput(string gptOutput)
        {
            return gptOutput.Replace("```json", "").Replace("```", "").Trim();
        }

        /// <summary>
        /// Maps the generic parsed JSON to a strongly-typed structure expected by the HTML template.
        /// Handles missing fields and uses defaults as needed.
        /// </summary>
        /// <param name="json">Parsed resume JObject</param>
        /// <returns>Anonymous object matching template keys</returns>
        private object TransformJsonKeys(JObject json)
        {
            // Load the logo file as base64 (adjust path if needed)
            string logoPath = Path.Combine("Images", LogoFile);
            string logoBase64 = File.Exists(logoPath)
                ? Convert.ToBase64String(File.ReadAllBytes(logoPath))
                : "";


            return new
            {
                Full_Name = json["Full Name"]?.ToString() ?? "",
                Title = json["Title"]?.ToString() ?? "",
                Email = json["Email"]?.ToString() ?? "",
                Phone_Number = json["Phone Number"]?.ToString() ?? "",
                LinkedIn = json["LinkedIn"]?.ToString() ?? "",
                Location = json["Location"]?.ToString() ?? "",
                Strengths = json["Strengths"]?.ToString() ?? "",
                Skill_Matrix = json["Skill Matrix"]?.ToObject<List<dynamic>>() ?? new List<dynamic>(),
                Key_Achievements = json["Key_Achievements"]?.ToObject<List<string>>() ?? new List<string>(),
                Education = json["Education"]?.ToObject<List<dynamic>>() ?? new List<dynamic>(),
                Projects = json["Projects"]?.ToObject<List<dynamic>>() ?? new List<dynamic>(),
                Certifications = json["Certifications"]?.ToObject<List<string>>() ?? new List<string>(),
                Software_Training = json["Software_Training"]?.ToString() ?? "",
                References = json["References"]?.ToObject<List<dynamic>>() ?? new List<dynamic>(),
                LogoBase64 = logoBase64
            };
        }

        /// <summary>
        /// Renders the structured resume model into HTML using the RazorEngine and a specified template.
        /// </summary>
        /// <param name="model">Structured resume object</param>
        /// <returns>Rendered HTML string</returns>
        private string RenderHtml(dynamic model)
        {
            string template = File.ReadAllText(TemplateFile);
            // "resumeTemplate" is the cache key for Razor; pass model as an object
            return Engine.Razor.RunCompile(template, "resumeTemplate", null, (object)model);
        }

        /// <summary>
        /// Converts rendered HTML into a Word (.docx) document using OpenXml and HtmlToOpenXml.
        /// </summary>
        /// <param name="html">HTML string to convert</param>
        /// <param name="outputPath">Path where .docx will be saved</param>
        private void GenerateDocxFromHtml(string html, string outputPath)
        {
            using MemoryStream stream = new();
            using WordprocessingDocument doc = WordprocessingDocument.Create(
                stream,
                DocumentFormat.OpenXml.WordprocessingDocumentType.Document,
                true
            );
            // Add a main document part to hold the content
            MainDocumentPart mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(
                new DocumentFormat.OpenXml.Wordprocessing.Body()
            );
            // HtmlConverter translates HTML/CSS to WordprocessingML elements
            HtmlConverter converter = new(mainPart);
            converter.ParseHtml(html);
            doc.Save();

            // Save the final DOCX file to disk
            File.WriteAllBytes(outputPath, stream.ToArray());
        }
    }
}
