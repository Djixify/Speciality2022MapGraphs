## Navigable network generation test

<html lang="en">
<head>
    <style>
    #image {
        display: block;
    }
    #gate {
        cursor: pointer;
        margin-bottom: 100px;
        width: 83px;
        height: 46px;
    }
    #ball {
        cursor: pointer;
        width: 40px;
        height: 40px;
    }
    </style>
</head>
<body>
  <div>
      <h1>GeeksforGeeks</h1>
      <h3>Click on the button to see image</h3>
      <img id="image" src=
          "https://localhost:44342/Map/generate/token=024b9d34348dd56d170f634e067274c6;dataset=geodanmark60%2Fvejmanhastigheder;bbox=586835.1,6135927.2,591812.3,6139738"
              alt="GFG image" />
  </div>

  <img src="https://en.js.cx/clipart/soccer-gate.svg" id="gate" class="droppable">
  
  <img src="https://en.js.cx/clipart/ball.svg" id="ball">
  
  <script>
      let currentDroppable = null;
      image.onmousedown = function(event) {
  
      let shiftX = event.clientX - ball.getBoundingClientRect().left;
      let shiftY = event.clientY - ball.getBoundingClientRect().top;
  
      ball.style.position = 'absolute';
      ball.style.zIndex = 1000;
      document.body.append(ball);
  
      moveAt(event.pageX, event.pageY);
  
      function moveAt(pageX, pageY) {
          ball.style.left = pageX - shiftX + 'px';
          ball.style.top = pageY - shiftY + 'px';
      }
  
      function onMouseMove(event) {
          moveAt(event.pageX, event.pageY);
  
          ball.hidden = true;
          let elemBelow = document.elementFromPoint(event.clientX, event.clientY);
          ball.hidden = false;
  
          if (!elemBelow) return;
  
          let droppableBelow = elemBelow.closest('.droppable');
          if (currentDroppable != droppableBelow) {
          if (currentDroppable) { // null when we were not over a droppable before this event
              leaveDroppable(currentDroppable);
          }
          currentDroppable = droppableBelow;
          if (currentDroppable) { // null if we're not coming over a droppable now
              // (maybe just left the droppable)
              enterDroppable(currentDroppable);
          }
          }
      }
  
      document.addEventListener('mousemove', onMouseMove);
  
      ball.onmouseup = function() {
          document.removeEventListener('mousemove', onMouseMove);
          ball.onmouseup = null;
      };
  
      };
  
      function enterDroppable(elem) {
      elem.style.background = 'pink';
      }
  
      function leaveDroppable(elem) {
      elem.style.background = '';
      }
  
      ball.ondragstart = function() {
      return false;
      };
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


