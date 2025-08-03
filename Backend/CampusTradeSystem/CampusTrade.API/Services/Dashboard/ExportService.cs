using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using iTextSharp.text;
using iTextSharp.text.pdf;

public class ExportService
{
    // 导出Excel
    public byte[] ExportToExcel<T>(List<T> data, string sheetName)
    {
        using var stream = new MemoryStream();
        var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet(sheetName);

        // 填充表头和数据（反射实现通用导出）
        var properties = typeof(T).GetProperties();
        var headerRow = sheet.CreateRow(0);

        for (int i = 0; i < properties.Length; i++)
        {
            headerRow.CreateCell(i).SetCellValue(properties[i].Name);
        }

        for (int i = 0; i < data.Count; i++)
        {
            var row = sheet.CreateRow(i + 1);
            for (int j = 0; j < properties.Length; j++)
            {
                var value = properties[j].GetValue(data[i])?.ToString() ?? "";
                row.CreateCell(j).SetCellValue(value);
            }
        }

        workbook.Write(stream);
        return stream.ToArray();
    }

    // 导出PDF（简化版）
    public byte[] ExportToPdf<T>(List<T> data, string title)
    {
        using var stream = new MemoryStream();
        var document = new Document();
        PdfWriter.GetInstance(document, stream);
        document.Open();

        // 添加标题
        document.Add(new Paragraph(title));
        document.Add(new Paragraph("\n"));

        // 添加表格
        var properties = typeof(T).GetProperties();
        var table = new PdfPTable(properties.Length);

        // 添加表头
        foreach (var prop in properties)
        {
            table.AddCell(prop.Name);
        }

        // 添加数据
        foreach (var item in data)
        {
            foreach (var prop in properties)
            {
                table.AddCell(prop.GetValue(item)?.ToString() ?? "");
            }
        }

        document.Add(table);
        document.Close();
        return stream.ToArray();
    }
}
