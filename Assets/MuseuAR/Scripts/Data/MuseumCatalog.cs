using System.Collections.Generic;
using UnityEngine;

namespace MuseuAR
{
    /// <summary>
    /// Lista de todas as peças disponíveis no app.
    /// Criar em: Assets > Create > Museu AR > Catálogo do Museu
    /// </summary>
    [CreateAssetMenu(fileName = "CatalogoDoMuseu", menuName = "Museu AR/Catálogo do Museu", order = 0)]
    public class MuseumCatalog : ScriptableObject
    {
        public string museumName = "Meu Museu";
        public List<MuseumItemData> items = new List<MuseumItemData>();

        [Header("Placas com QR code")]
        [Tooltip("O que o QR das placas abre na câmera comum do celular. {id} = id da peça.\n" +
                 "Ex.: https://meumuseu.org/app  ou  https://meumuseu.org/pecas/{id}\n" +
                 "Depois de mudar, rode Museu AR > 3. Gerar placas com QR code e reimprima.")]
        public string qrContent = "MUSEUAR:{id}";

        public string QrContentFor(MuseumItemData item) =>
            (qrContent ?? "").Replace("{id}", item ? item.id : "");

        public MuseumItemData FindByImageName(string imageName)
        {
            if (string.IsNullOrEmpty(imageName)) return null;
            foreach (var item in items)
                if (item && item.referenceImageName == imageName) return item;
            return null;
        }

        public MuseumItemData FindById(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            foreach (var item in items)
                if (item && item.id == itemId) return item;
            return null;
        }
    }
}
