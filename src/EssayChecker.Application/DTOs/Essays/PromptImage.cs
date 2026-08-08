namespace EssayChecker.Application.DTOs.Essays;

/// <summary>
/// 9-cu sinifdə tələbəyə verilən 3 promt-şəklindən biri (yazı tapşırığının əsaslandığı şəkillər).
/// Stream deyil, byte[] saxlanılır ki, dəyər bir dəfədən çox (AI çağırışı + lazım gələrsə log) oxuna bilsin.
/// </summary>
public sealed record PromptImage(byte[] Data, string ContentType);
