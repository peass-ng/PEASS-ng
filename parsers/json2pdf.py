#!/usr/bin/env python3
import sys
import json
import html
from reportlab.lib.pagesizes import letter
from reportlab.platypus import Frame, Paragraph, Spacer, PageBreak,PageTemplate, BaseDocTemplate
from reportlab.platypus.tableofcontents import TableOfContents
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import cm

styles = getSampleStyleSheet()
text_colors = { "GREEN": "#00DB00", "RED": "#FF0000", "REDYELLOW": "#FFA500", "BLUE": "#0000FF",
    "DARKGREY": "#5C5C5C", "YELLOW": "#ebeb21", "MAGENTA": "#FF00FF", "CYAN": "#00FFFF", "LIGHT_GREY": "#A6A6A6"}

# Required to automatically set Page Numbers
class PageTemplateWithCount(PageTemplate):
    def __init__(self, id, frames, **kw):
        PageTemplate.__init__(self, id, frames, **kw)

    def beforeDrawPage(self, canvas, doc):
        page_num = canvas.getPageNumber()
        canvas.drawRightString(10.5*cm, 1*cm, str(page_num))

# Required to automatically set the Table of Contents
class MyDocTemplate(BaseDocTemplate):
    def __init__(self, filename, **kw):
        self.allowSplitting = 0
        BaseDocTemplate.__init__(self, filename, **kw)
        template = PageTemplateWithCount("normal", [Frame(2.5*cm, 2.5*cm, 15*cm, 25*cm, id='F1')])
        self.addPageTemplates(template)

    def afterFlowable(self, flowable):
        if flowable.__class__.__name__ == "Paragraph":
            text = flowable.getPlainText()
            style = flowable.style.name
            if style == "Heading1":
                self.notify("TOCEntry", (0, text, self.page))
            if style == "Heading2":
                self.notify("TOCEntry", (1, text, self.page))
            if style == "Heading3":
                self.notify("TOCEntry", (2, text, self.page))
      

# Poor take at dynamicly generating styles depending on depth(?)
def get_level_styles(level):
    global styles
    indent_value = 10 * (level - 1);
    # Overriding some default stylings
    level_styles = { 
        "title": ParagraphStyle(
          **dict(styles[f"Heading{level}"].__dict__,
          **{ "leftIndent": indent_value })),
        "text": ParagraphStyle(
          **dict(styles["Code"].__dict__,
          **{ "backColor": "#F0F0F0",
          "borderPadding": 5, "borderWidth": 1,
          "borderColor": "black", "borderRadius": 5,
          "leftIndent": 5 + indent_value})),
        "info": ParagraphStyle(
          **dict(styles["Italic"].__dict__,
          **{ "leftIndent": indent_value })),
    }
    return level_styles

def get_colors_by_text(colors):
    new_colors = {}
    for (color, words) in colors.items():
        for word in words:
            new_colors[html.escape(word)] = color
    return new_colors

def build_main_section(section, title, level=1):
    styles = get_level_styles(level)
    has_links = "infos" in section.keys() and len(section["infos"]) > 0
    has_lines = "lines" in section.keys() and len(section["lines"]) > 1
    has_children = "sections" in section.keys() and len(section["sections"].keys()) > 0

    # Only display data for Sections with results
    show_section = has_lines or has_children

    elements = []

    if show_section:
        elements.append(Paragraph(title, style=styles["title"]))

    # Print info if any
    if show_section and has_links:
        for info in section["infos"]:
            words = info.split() 
            # Join all lines and encode any links that might be present.
            words = map(lambda word: f'<a href="{word}" color="blue">{word}</a>' if "http" in word else word, words)
            words = " ".join(words)
            elements.append(Paragraph(words, style=styles["info"] ))

  # Print lines if any
    if "lines" in section.keys() and len(section["lines"]) > 1:
        colors_by_line = list(map(lambda x: x["colors"], section["lines"]))
        lines = list(map(lambda x: html.escape(x["clean_text"]), section["lines"]))
        for (idx, line) in enumerate(lines):
            colors = colors_by_line[idx]
            colored_text = get_colors_by_text(colors)
            colored_line = line
            for (text, color) in colored_text.items():
                if color == "REDYELLOW":
                    colored_line = colored_line.replace(text, f'<font color="{text_colors[color]}"><b>{text}</b></font>')
                else:
                    colored_line = colored_line.replace(text, f'<font color="{text_colors[color]}">{text}</font>')
            lines[idx] = colored_line
        elements.append(Spacer(0, 10))
        line = "<br/>".join(lines)

    # If it's a top level entry remove the line break caused by an empty "clean_text"
        if level == 1: line = line[5:]
        elements.append(Paragraph(line, style=styles["text"]))


  # Print child sections
    if has_children:
        for child_title in section["sections"].keys():
            element_list = build_main_section(section["sections"][child_title], child_title, level + 1)
            elements.extend(element_list)
  
  # Add spacing at the end of section. The deeper the level the smaller the spacing.
    if show_section:
        elements.append(Spacer(1, 40 - (10 * level)))
  
    return elements
  

def main():
    with open(JSON_PATH) as file:
        # Read and parse JSON file
        data = json.loads(file.read())

        # Default pdf values
        doc = MyDocTemplate(PDF_PATH)
        toc = TableOfContents()
        toc.levelStyles = [
            ParagraphStyle(name = "Heading1", fontSize = 14, leading=16),
            ParagraphStyle(name = "Heading2", fontSize = 12, leading=14, leftIndent = 10),
            ParagraphStyle(name = "Heading3", fontSize = 10, leading=12, leftIndent = 20),
        ]

        elements = [Paragraph("PEAS Report", style=styles["Title"]), Spacer(0, 30), toc, PageBreak()]
      
        # Iterate over all top level sections and build their elements.
        for title in data.keys():
            element_list = build_main_section(data[title], title)
            elements.extend(element_list)
      
        doc.multiBuild(elements)

# Start execution
if __name__ == "__main__":
    try:
        JSON_PATH = sys.argv[1]
        PDF_PATH = sys.argv[2]
    except IndexError as err:
        print("Error: Please pass the peas.json file and the path to save the pdf\njson2pdf.py <json_file> <pdf_file.pdf>")
        sys.exit(1)
    
    main()
