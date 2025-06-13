from markitdown import MarkItDown
import httpx
import tempfile
import os
import asyncio
from fastapi import HTTPException

MAX_PDF_SIZE_BYTES = 2 * 1024 * 1024  # 2MB

async def scrape_pdf_with_markitdown(url: str) -> str:
    """
    Downloads a PDF from the given URL, checks its size, 
    and converts it to markdown using MarkItDown.
    """
    md = MarkItDown(enable_plugins=False)

    async with httpx.AsyncClient() as client:
        response = await client.get(url, follow_redirects=True)
        response.raise_for_status()

        content_length_str = response.headers.get("Content-Length")
        if content_length_str:
            content_length = int(content_length_str)
            if content_length > MAX_PDF_SIZE_BYTES:
                raise HTTPException(status_code=413,
                                    detail=f"PDF file at {url} is too large ({content_length / (1024*1024):.2f}MB). Maximum allowed size is {MAX_PDF_SIZE_BYTES / (1024*1024):.2f}MB.")

        pdf_content = await response.aread()

        if not content_length_str and len(pdf_content) > MAX_PDF_SIZE_BYTES:
            raise HTTPException(status_code=413,
                                detail=f"PDF file at {url} is too large (downloaded size {len(pdf_content) / (1024*1024):.2f}MB). Maximum allowed size is {MAX_PDF_SIZE_BYTES / (1024*1024):.2f}MB.")

        with tempfile.NamedTemporaryFile(delete=False, suffix=".pdf") as tmp_file:
            tmp_file.write(pdf_content)
            tmp_file_path = tmp_file.name
    
    try:
        # Run the synchronous MarkItDown conversion in a thread pool executor
        loop = asyncio.get_event_loop()
        markdown_result = await loop.run_in_executor(None, md.convert, tmp_file_path)
    finally:
        os.remove(tmp_file_path)
            
    return markdown_result.text_content
