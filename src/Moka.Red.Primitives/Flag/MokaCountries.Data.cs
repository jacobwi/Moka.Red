namespace Moka.Red.Primitives.Flag;

public static partial class MokaCountries
{
	// One country a line: code|English name|own name|dialing codes|region|other names. English names come from
	// RegionInfo and own names from each region's default culture, read once on Windows, where ICU has full data;
	// a WebAssembly app with invariant globalization would get no real names from RegionInfo. Dialing codes are
	// written by hand and were checked against Windows and CLDR's telephone codes. Regions follow UN M49: F Africa,
	// M Americas, N Antarctica, S Asia, E Europe, O Oceania.
	private const string Data = """
		AD|Andorra|Andorra|+376|E|
		AE|United Arab Emirates|الإمارات العربية المتحدة|+971|S|UAE;Emirates
		AF|Afghanistan|افغانستان|+93|S|
		AG|Antigua & Barbuda|Antigua & Barbuda|+1|M|Antigua and Barbuda
		AI|Anguilla|Anguilla|+1|M|
		AL|Albania|Shqipëri|+355|E|
		AM|Armenia|Հայաստան|+374|S|
		AO|Angola|Angola|+244|F|
		AQ|Antarctica|Antarctica|+672|N|
		AR|Argentina|Argentina|+54|M|
		AS|American Samoa|American Samoa|+1|O|
		AT|Austria|Österreich|+43|E|
		AU|Australia|Australia|+61|O|
		AW|Aruba|Aruba|+297|M|
		AX|Åland Islands|Åland|+358|E|
		AZ|Azerbaijan|Azərbaycan|+994|S|
		BA|Bosnia & Herzegovina|Bosna i Hercegovina|+387|E|Bosnia;Bosnia and Herzegovina
		BB|Barbados|Barbados|+1|M|
		BD|Bangladesh|বাংলাদেশ|+880|S|
		BE|Belgium|België, Belgique, Belgien|+32|E|
		BF|Burkina Faso|Burkina Faso|+226|F|
		BG|Bulgaria|България|+359|E|
		BH|Bahrain|البحرين|+973|S|
		BI|Burundi|Uburundi|+257|F|
		BJ|Benin|Bénin|+229|F|
		BL|St. Barthélemy|Saint-Barthélemy|+590|M|Saint Barthélemy
		BM|Bermuda|Bermuda|+1|M|
		BN|Brunei|Brunei|+673|S|
		BO|Bolivia|Bolivia|+591|M|
		BQ|Bonaire, Sint Eustatius and Saba|Bonaire, Sint Eustatius en Saba|+599|M|Caribbean Netherlands;Bonaire;Sint Eustatius;Saba
		BR|Brazil|Brasil|+55|M|
		BS|Bahamas|Bahamas|+1|M|
		BT|Bhutan|འབྲུག|+975|S|
		BV|Bouvet Island|Bouvetøya||M|
		BW|Botswana|Botswana|+267|F|
		BY|Belarus|Беларусь|+375|E|
		BZ|Belize|Belize|+501|M|
		CA|Canada|Canada|+1|M|
		CC|Cocos (Keeling) Islands|Cocos (Keeling) Islands|+61|O|
		CD|Congo (DRC)|Congo (République démocratique du)|+243|F|DRC;DR Congo;Democratic Republic of the Congo;Congo-Kinshasa;Zaire
		CF|Central African Republic|République centrafricaine|+236|F|
		CG|Congo (Republic)|Congo|+242|F|Republic of the Congo;Congo-Brazzaville;Congo
		CH|Switzerland|Schweiz, Suisse, Svizzera|+41|E|Swiss
		CI|Côte d’Ivoire|Côte d’Ivoire|+225|F|Ivory Coast
		CK|Cook Islands|Cook Islands|+682|O|
		CL|Chile|Chile|+56|M|
		CM|Cameroon|Cameroun|+237|F|
		CN|China|中国|+86|S|PRC
		CO|Colombia|Colombia|+57|M|
		CR|Costa Rica|Costa Rica|+506|M|
		CU|Cuba|Cuba|+53|M|
		CV|Cabo Verde|Cabo Verde|+238|F|Cape Verde
		CW|Curaçao|Curaçao|+599|M|
		CX|Christmas Island|Christmas Island|+61|O|
		CY|Cyprus|Κύπρος, Kıbrıs|+357|S|
		CZ|Czechia|Česko|+420|E|Czech Republic
		DE|Germany|Deutschland|+49|E|
		DJ|Djibouti|Djibouti|+253|F|
		DK|Denmark|Danmark|+45|E|
		DM|Dominica|Dominica|+1|M|
		DO|Dominican Republic|República Dominicana|+1|M|
		DZ|Algeria|الجزائر|+213|F|
		EC|Ecuador|Ecuador|+593|M|
		EE|Estonia|Eesti|+372|E|
		EG|Egypt|مصر|+20|F|
		EH|Western Sahara|الصحراء الغربية|+212|F|
		ER|Eritrea|ኤርትራ|+291|F|
		ES|Spain|España|+34|E|
		ET|Ethiopia|ኢትዮጵያ|+251|F|
		FI|Finland|Suomi|+358|E|
		FJ|Fiji|Viti|+679|O|
		FK|Falkland Islands|Falkland Islands|+500|M|Falklands;Malvinas
		FM|Micronesia|Micronesia|+691|O|Federated States of Micronesia
		FO|Faroe Islands|Føroyar|+298|E|
		FR|France|France|+33|E|
		GA|Gabon|Gabon|+241|F|
		GB|United Kingdom|United Kingdom|+44|E|UK;Britain;Great Britain;England;Scotland;Wales;Northern Ireland
		GD|Grenada|Grenada|+1|M|
		GE|Georgia|საქართველო|+995|S|
		GF|French Guiana|Guyane française|+594|M|
		GG|Guernsey|Guernsey|+44|E|
		GH|Ghana|Ghana|+233|F|
		GI|Gibraltar|Gibraltar|+350|E|
		GL|Greenland|Kalaallit Nunaat|+299|M|
		GM|Gambia|Gambia|+220|F|
		GN|Guinea|Guinée|+224|F|
		GP|Guadeloupe|Guadeloupe|+590|M|
		GQ|Equatorial Guinea|Guinea Ecuatorial|+240|F|
		GR|Greece|Ελλάδα|+30|E|
		GS|South Georgia & South Sandwich Islands|South Georgia & South Sandwich Islands|+500|M|South Georgia and South Sandwich Islands;South Georgia and the South Sandwich Islands
		GT|Guatemala|Guatemala|+502|M|
		GU|Guam|Guam|+1|O|
		GW|Guinea-Bissau|Guiné-Bissau|+245|F|
		GY|Guyana|Guyana|+592|M|
		HK|Hong Kong|香港|+852|S|Hong Kong SAR
		HM|Heard & McDonald Islands|Heard & McDonald Islands||O|Heard and McDonald Islands;Heard Island and McDonald Islands
		HN|Honduras|Honduras|+504|M|
		HR|Croatia|Hrvatska|+385|E|
		HT|Haiti|Haïti|+509|M|
		HU|Hungary|Magyarország|+36|E|
		ID|Indonesia|Indonesia|+62|S|
		IE|Ireland|Éire|+353|E|
		IL|Israel|ישראל|+972|S|
		IM|Isle of Man|Ellan Vannin|+44|E|
		IN|India|भारत|+91|S|
		IO|British Indian Ocean Territory|British Indian Ocean Territory|+246|F|
		IQ|Iraq|العراق|+964|S|
		IR|Iran|ایران|+98|S|Persia
		IS|Iceland|Ísland|+354|E|
		IT|Italy|Italia|+39|E|
		JE|Jersey|Jersey|+44|E|
		JM|Jamaica|Jamaica|+1|M|
		JO|Jordan|الأردن|+962|S|
		JP|Japan|日本|+81|S|
		KE|Kenya|Kenya|+254|F|
		KG|Kyrgyzstan|Кыргызстан|+996|S|
		KH|Cambodia|កម្ពុជា|+855|S|
		KI|Kiribati|Kiribati|+686|O|
		KM|Comoros|جزر القمر|+269|F|
		KN|St. Kitts & Nevis|St. Kitts & Nevis|+1|M|St. Kitts and Nevis;Saint Kitts and Nevis
		KP|North Korea|조선민주주의인민공화국|+850|S|DPRK
		KR|South Korea|대한민국|+82|S|Korea;Republic of Korea
		KW|Kuwait|الكويت|+965|S|
		KY|Cayman Islands|Cayman Islands|+1|M|
		KZ|Kazakhstan|Қазақстан|+7|S|
		LA|Laos|ລາວ|+856|S|Lao
		LB|Lebanon|لبنان|+961|S|
		LC|St. Lucia|St. Lucia|+1|M|Saint Lucia
		LI|Liechtenstein|Liechtenstein|+423|E|
		LK|Sri Lanka|ශ්‍රී ලංකාව|+94|S|
		LR|Liberia|Liberia|+231|F|
		LS|Lesotho|Lesotho|+266|F|
		LT|Lithuania|Lietuva|+370|E|
		LU|Luxembourg|Lëtzebuerg, Luxembourg, Luxemburg|+352|E|
		LV|Latvia|Latvija|+371|E|
		LY|Libya|ليبيا|+218|F|
		MA|Morocco|المغرب|+212|F|
		MC|Monaco|Monaco|+377|E|
		MD|Moldova|Republica Moldova|+373|E|
		ME|Montenegro|Crna Gora|+382|E|
		MF|St. Martin|Saint-Martin|+590|M|Saint Martin
		MG|Madagascar|Madagasikara|+261|F|
		MH|Marshall Islands|Marshall Islands|+692|O|
		MK|North Macedonia|Северна Македонија|+389|E|Macedonia
		ML|Mali|Mali|+223|F|
		MM|Myanmar|မြန်မာ|+95|S|Burma
		MN|Mongolia|Монгол|+976|S|
		MO|Macao|澳門|+853|S|Macau;Macao SAR
		MP|Northern Mariana Islands|Northern Mariana Islands|+1|O|
		MQ|Martinique|Martinique|+596|M|
		MR|Mauritania|موريتانيا|+222|F|
		MS|Montserrat|Montserrat|+1|M|
		MT|Malta|Malta|+356|E|
		MU|Mauritius|Mauritius|+230|F|
		MV|Maldives|Maldives|+960|S|
		MW|Malawi|Malawi|+265|F|
		MX|Mexico|México|+52|M|
		MY|Malaysia|Malaysia|+60|S|
		MZ|Mozambique|Moçambique|+258|F|
		NA|Namibia|Namibia|+264|F|
		NC|New Caledonia|Nouvelle-Calédonie|+687|O|
		NE|Niger|Niger|+227|F|
		NF|Norfolk Island|Norfolk Island|+672|O|
		NG|Nigeria|Nigeria|+234|F|
		NI|Nicaragua|Nicaragua|+505|M|
		NL|Netherlands|Nederland|+31|E|Holland
		NO|Norway|Norge|+47|E|
		NP|Nepal|नेपाल|+977|S|
		NR|Nauru|Nauru|+674|O|
		NU|Niue|Niue|+683|O|
		NZ|New Zealand|Aotearoa|+64|O|
		OM|Oman|عُمان|+968|S|
		PA|Panama|Panamá|+507|M|
		PE|Peru|Perú|+51|M|
		PF|French Polynesia|Polynésie française|+689|O|
		PG|Papua New Guinea|Papua New Guinea|+675|O|
		PH|Philippines|Pilipinas|+63|S|
		PK|Pakistan|پاکستان|+92|S|
		PL|Poland|Polska|+48|E|
		PM|St. Pierre & Miquelon|Saint-Pierre-et-Miquelon|+508|M|St. Pierre and Miquelon;Saint Pierre and Miquelon
		PN|Pitcairn Islands|Pitcairn Islands|+64|O|
		PR|Puerto Rico|Puerto Rico|+1|M|
		PS|Palestinian Territories|فلسطين|+970|S|Palestine;Palestinian Authority
		PT|Portugal|Portugal|+351|E|
		PW|Palau|Palau|+680|O|
		PY|Paraguay|Paraguay|+595|M|
		QA|Qatar|قطر|+974|S|
		RE|Réunion|La Réunion|+262|F|
		RO|Romania|România|+40|E|
		RS|Serbia|Srbija|+381|E|
		RU|Russia|Россия|+7|E|Russian Federation
		RW|Rwanda|U Rwanda|+250|F|
		SA|Saudi Arabia|المملكة العربية السعودية|+966|S|KSA
		SB|Solomon Islands|Solomon Islands|+677|O|
		SC|Seychelles|Seychelles|+248|F|
		SD|Sudan|السودان|+249|F|
		SE|Sweden|Sverige|+46|E|
		SG|Singapore|Singapore|+65|S|
		SH|St. Helena, Ascension & Tristan da Cunha|St. Helena|+290 +247|F|Saint Helena;Ascension Island;Tristan da Cunha;St. Helena, Ascension and Tristan da Cunha;Saint Helena, Ascension and Tristan da Cunha
		SI|Slovenia|Slovenija|+386|E|
		SJ|Svalbard & Jan Mayen|Svalbard og Jan Mayen|+47|E|Svalbard and Jan Mayen;Svalbard
		SK|Slovakia|Slovensko|+421|E|
		SL|Sierra Leone|Sierra Leone|+232|F|
		SM|San Marino|San Marino|+378|E|
		SN|Senegal|Sénégal|+221|F|
		SO|Somalia|Soomaaliya|+252|F|
		SR|Suriname|Suriname|+597|M|
		SS|South Sudan|South Sudan|+211|F|
		ST|São Tomé & Príncipe|São Tomé e Príncipe|+239|F|São Tomé and Príncipe
		SV|El Salvador|El Salvador|+503|M|
		SX|Sint Maarten|Sint-Maarten|+1|M|
		SY|Syria|سوريا|+963|S|
		SZ|Eswatini|eSwatini|+268|F|Swaziland
		TC|Turks & Caicos Islands|Turks & Caicos Islands|+1|M|Turks and Caicos Islands
		TD|Chad|Tchad|+235|F|
		TF|French Southern Territories|Terres australes françaises|+262|F|
		TG|Togo|Togo|+228|F|
		TH|Thailand|ไทย|+66|S|
		TJ|Tajikistan|Тоҷикистон|+992|S|
		TK|Tokelau|Tokelau|+690|O|
		TL|Timor-Leste|Timor-Leste|+670|S|East Timor
		TM|Turkmenistan|Türkmenistan|+993|S|
		TN|Tunisia|تونس|+216|F|
		TO|Tonga|Tonga|+676|O|
		TR|Türkiye|Türkiye|+90|S|Turkey
		TT|Trinidad & Tobago|Trinidad & Tobago|+1|M|Trinidad and Tobago
		TV|Tuvalu|Tuvalu|+688|O|
		TW|Taiwan|台灣|+886|S|Republic of China
		TZ|Tanzania|Tanzania|+255|F|
		UA|Ukraine|Україна|+380|E|
		UG|Uganda|Uganda|+256|F|
		UM|U.S. Outlying Islands|U.S. Outlying Islands|+1|O|U.S. Minor Outlying Islands
		US|United States|United States|+1|M|USA;America;United States of America
		UY|Uruguay|Uruguay|+598|M|
		UZ|Uzbekistan|Oʻzbekiston|+998|S|
		VA|Vatican City|Città del Vaticano|+39|E|Vatican;Holy See
		VC|St. Vincent & Grenadines|St. Vincent & Grenadines|+1|M|St. Vincent and Grenadines;Saint Vincent and Grenadines;Saint Vincent and the Grenadines
		VE|Venezuela|Venezuela|+58|M|
		VG|British Virgin Islands|British Virgin Islands|+1|M|BVI
		VI|U.S. Virgin Islands|U.S. Virgin Islands|+1|M|USVI
		VN|Vietnam|Việt Nam|+84|S|
		VU|Vanuatu|Vanuatu|+678|O|
		WF|Wallis & Futuna|Wallis-et-Futuna|+681|O|Wallis and Futuna
		WS|Samoa|Samoa|+685|O|
		XK|Kosovo|Kosovë|+383|E|
		YE|Yemen|اليمن|+967|S|
		YT|Mayotte|Mayotte|+262|F|
		ZA|South Africa|South Africa|+27|F|
		ZM|Zambia|Zambia|+260|F|
		ZW|Zimbabwe|Zimbabwe|+263|F|
		""";
}