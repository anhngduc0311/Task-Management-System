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
