namespace UdemyBagisSistemi.Servisler;

public interface IEpostaServisi
{
    Task EpostaGonderAsync(string kime, string konu, string icerik);
}
