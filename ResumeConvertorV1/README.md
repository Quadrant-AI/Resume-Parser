# ResumeConvertorV1

This tool reads `.pdf` or `.txt` resumes, uses OpenAI to extract structured information, and generates a clean, formatted `.html` and `.docx` resume.

---

## What This Tool Does

- Reads resumes from the `resumes/` folder  
- Uses OpenAI's Chat API and a custom system prompt to extract:
  - Skills, Projects, Education, Certifications, Strengths, etc.
- Renders a structured resume using Razor templating
- Exports the final resume to:
  - `Output/{filename}.html`
  - `Output/{filename}.docx`

---

## How to Set Up

### 1. Clone or Download the Project

```bash
git clone <your-repo-url>
```

Or download and open in Visual Studio / JetBrains Rider.

---

### 2. Install Dependencies

Make sure the following NuGet packages are installed:

- `HtmlAgilityPack`  
- `HtmlToOpenXml`  
- `DocumentFormat.OpenXml`  
- `Microsoft.Extensions.Configuration`  
- `Microsoft.Extensions.Configuration.Json`  
- `RazorEngine`  
- `PdfPig`  
- `Newtonsoft.Json`

---

### 3. Configure `appsettings.json`

This file is located in the project root.

```json
{
  "OpenAI": {
    "ApiKey": "YOUR_OPENAI_API_KEY_HERE",
    "Model": "gpt-4o"
  },
  "ResumeConvertor": {
    "TemplateFile": "ResumeTargetTemplate.html",
    "PromptFile": "SystemPrompt_Resume.txt",
    "OutputDirectory": "D:\Path\To\Your\Output\Folder"
  }
}
```

####  What You Must Update:

| Key | Description |
|-----|-------------|
| `"ApiKey"` | Your personal OpenAI API key (required) |
| `"Model"` | GPT model name (default: `gpt-4o`) |
| `"OutputDirectory"` | Folder where the output files will be saved |

---

### 4. Set the Resume Folder

In `Program.cs`, update the following line pointing to the folder containing source resumes which needs to be converted

```csharp
string resumeFolder = @"D:\Source\Work Projects\ResumeConvertorV1\ResumeConvertorV1\resumes";
```

> This is the folder where you must place all your `.pdf` or `.txt` resumes.

If this folder doesn’t exist or is empty, the program will skip processing.

---

##  How to Run

Build the solution and run `Program.cs`.  
It will loop through all valid resumes and generate output files.

---

##  Output Location

Files will be saved to the folder specified in `OutputDirectory` from `appsettings.json`.

Each resume will generate:

- `Output/filename.html`
- `Output/filename.docx`

---

## How It Works (Behind the Scenes)

1. Reads the resume file (.pdf or .txt)
2. Sends the raw text + prompt to OpenAI
3. Receives structured JSON response
4. Maps it into a typed object
5. Renders HTML using a Razor template
6. Converts HTML to Word using OpenXML
7. Saves both files to the `OutputDirectory`

---

## Notes

- The GPT parsing behavior is driven by `SystemPrompt_Resume.txt`
- Resume formatting is defined in `ResumeTargetTemplate.html`
- Make sure your internet connection allows access to the OpenAI API

---

## Example Output

```
Output/
├── JohnDoe.html
└── JohnDoe.docx
```