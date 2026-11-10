package io.github.enestahtaci.enestv

/** Spike-only test data: a large, Turkish-named channel list without needing a real provider. */
object SyntheticChannels {
    private val names = arrayOf(
        "ŞİMDİ TV", "İSTANBUL HABER", "Isparta Haber", "Çocuk Kanalı", "Ağaç Belgesel",
        "Spor Max", "Öğrenci TV", "Gündem 24", "Müzik Türk", "Sinema Aile",
    )

    fun create(count: Int): Array<String> = Array(count) { i -> "${names[i % names.size]} ${i / names.size + 1}" }
}
