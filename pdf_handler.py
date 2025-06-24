from markitdown import MarkItDown
import httpx
import tempfile
import os
import asyncio
from fastapi import HTTPException
from concurrent.futures import ThreadPoolExecutor

MAX_PDF_SIZE_BYTES = 10 * 1024 * 1024  # 10 MB

# Create a ThreadPoolExecutor for PDF processing.
pdf_executor = ThreadPoolExecutor(max_workers=11)

# Semaphore to limit concurrent PDF processing tasks
MAX_CONCURRENT_PDF_TASKS = 10
pdf_processing_semaphore = asyncio.Semaphore(MAX_CONCURRENT_PDF_TASKS)

def get_count_of_pdf_processing_tasks() -> int:
    """
    Returns the current count of PDF processing tasks.
    This is useful for monitoring and debugging purposes.
    """
    return MAX_CONCURRENT_PDF_TASKS - pdf_processing_semaphore._value  # Accessing the semaphore's internal value directly

async def scrape_pdf_with_markitdown(url: str, timeout_seconds: int) -> str:
    """
    Downloads a PDF from the given URL, checks its size,
    and converts it to markdown using MarkItDown, limiting concurrency.
    """
    md = MarkItDown(enable_plugins=False)

    async with httpx.AsyncClient() as client:
        response = await client.get(url, follow_redirects=True, timeout=float(timeout_seconds))
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

    async with pdf_processing_semaphore: # Acquire semaphore
        try:
            loop = asyncio.get_event_loop()
            # Run the synchronous MarkItDown conversion in the custom thread pool executor
            markdown_result = await loop.run_in_executor(pdf_executor, md.convert, tmp_file_path)
        finally:
            os.remove(tmp_file_path)

    return markdown_result.text_content
