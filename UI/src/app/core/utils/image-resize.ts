/**
 * Shrinks any image the browser can open (PNG, JPEG, WEBP, GIF, SVG, BMP, ...) to a small data URL:
 * the longest side is scaled down to `maxSide` px, then it is saved as PNG (best for emails) or, when the PNG is
 * too big, as WEBP with lower quality, shrinking further until it fits `maxLength` characters. Transparency is kept.
 */
export async function shrinkImage(file: File, maxSide = 512, maxLength = 250_000): Promise<string> {
  const url = URL.createObjectURL(file);
  try {
    const image = await loadImage(url);
    // SVGs without width/height report 0
    const width = image.naturalWidth || maxSide;
    const height = image.naturalHeight || maxSide;

    let side = maxSide;
    for (let attempt = 0; attempt < 8; attempt++) {
      const scale = Math.min(1, side / Math.max(width, height));
      const canvas = document.createElement('canvas');
      canvas.width = Math.max(1, Math.round(width * scale));
      canvas.height = Math.max(1, Math.round(height * scale));

      const context = canvas.getContext('2d');
      if (!context) throw new Error('Canvas is not available');
      context.imageSmoothingQuality = 'high';
      context.drawImage(image, 0, 0, canvas.width, canvas.height);

      // PNG first: it shows in every mail app (Outlook for Windows can't show WEBP in emails).
      const png = canvas.toDataURL('image/png');
      if (png.length <= maxLength) return png;

      for (const quality of [0.92, 0.85, 0.75, 0.65]) {
        const data = canvas.toDataURL('image/webp', quality);
        if (data.startsWith('data:image/webp') && data.length <= maxLength) return data;
      }
      side = Math.round(side * 0.75);
    }
    throw new Error('Image is still too large');
  } finally {
    URL.revokeObjectURL(url);
  }
}

function loadImage(url: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const image = new Image();
    image.onload = () => resolve(image);
    image.onerror = () => reject(new Error('The file is not an image the browser can open'));
    image.src = url;
  });
}
