import { Pipe, PipeTransform, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

@Pipe({
  name: 'safeHtml',
  standalone: true
})
export class SafeHtmlPipe implements PipeTransform {
  private readonly sanitizer = inject(DomSanitizer);

  transform(html: string | null | undefined): SafeHtml {
    if (!html) return '';

    let processedHtml = html;
    try {
      // Parse HTML with built-in DOMParser to safely inspect and modify links
      const parser = new DOMParser();
      const doc = parser.parseFromString(html, 'text/html');

      // Helper function to linkify text nodes recursively without breaking existing HTML tags
      const linkifyTextNode = (node: Node) => {
        let parent = node.parentNode;
        while (parent) {
          if (parent.nodeName === 'A') return;
          parent = parent.parentNode;
        }

        if (node.nodeType === Node.TEXT_NODE && node.nodeValue) {
          const text = node.nodeValue;
          const urlRegex = /(https?:\/\/[^\s<]+[^.,\s<]|www\.[^\s<]+[^.,\s<])/g;
          if (urlRegex.test(text)) {
            const tempDiv = document.createElement('div');
            tempDiv.innerHTML = text.replace(urlRegex, (url) => {
              const href = url.startsWith('http') ? url : `https://${url}`;
              return `<a href="${href}" target="_blank" rel="noopener noreferrer">${url}</a>`;
            });
            
            const parentNode = node.parentNode;
            if (parentNode) {
              while (tempDiv.firstChild) {
                parentNode.insertBefore(tempDiv.firstChild, node);
              }
              parentNode.removeChild(node);
            }
          }
        } else {
          const children = Array.from(node.childNodes);
          children.forEach(child => linkifyTextNode(child));
        }
      };

      linkifyTextNode(doc.body);

      const links = doc.querySelectorAll('a');
      links.forEach(link => {
        link.setAttribute('target', '_blank');
        link.setAttribute('rel', 'noopener noreferrer');
      });
      processedHtml = doc.body.innerHTML;
    } catch (e) {
      // Fallback in case DOMParser is not available or throws
    }

    return this.sanitizer.bypassSecurityTrustHtml(processedHtml);
  }
}
