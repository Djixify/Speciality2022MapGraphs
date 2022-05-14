## Navigable network generation test

<html lang="en">
<head>
    <style>
 
    /* Set display to none for image*/
    #image {
        display: none;
    }
    </style>
</head>
<body>
    <div>
        <h1>GeeksforGeeks</h1>
        <h3>Click on the button to see image</h3>
        <img id="image" src=
            "https://media.geeksforgeeks.org/wp-content/uploads/20210915115837/gfg3.png"
                alt="GFG image" />
    </div>

    <button type="button"
        onclick="show()" id="btnID">
        Show Image
    </button>

    <script>
        function show() {

            /* Access image by id and change
            the display property to block*/
            document.getElementById('image')
                    .style.display = "block";

            document.getElementById('btnID')
                    .style.display = "none";
        }
    </script>
</body>
</html>

## Welcome to GitHub Pages

You can use the [editor on GitHub](https://github.com/Djixify/Speciality2022MapGraphs/edit/gh-pages/index.md) to maintain and preview the content for your website in Markdown files.

Whenever you commit to this repository, GitHub Pages will run [Jekyll](https://jekyllrb.com/) to rebuild the pages in your site, from the content in your Markdown files.

### Markdown

Markdown is a lightweight and easy-to-use syntax for styling your writing. It includes conventions for

```markdown
Syntax highlighted code block

# Header 1
## Header 2
### Header 3

- Bulleted
- List

1. Numbered
2. List

**Bold** and _Italic_ and `Code` text

[Link](url) and ![Image](src)
```

For more details see [Basic writing and formatting syntax](https://docs.github.com/en/github/writing-on-github/getting-started-with-writing-and-formatting-on-github/basic-writing-and-formatting-syntax).

### Jekyll Themes

Your Pages site will use the layout and styles from the Jekyll theme you have selected in your [repository settings](https://github.com/Djixify/Speciality2022MapGraphs/settings/pages). The name of this theme is saved in the Jekyll `_config.yml` configuration file.

### Support or Contact

Having trouble with Pages? Check out our [documentation](https://docs.github.com/categories/github-pages-basics/) or [contact support](https://support.github.com/contact) and we’ll help you sort it out.


